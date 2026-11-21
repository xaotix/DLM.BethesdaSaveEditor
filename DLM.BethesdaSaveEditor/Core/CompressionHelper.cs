using System;
using System.IO;
using System.IO.Compression;

namespace DLM.BethesdaSaveEditor.Core
{
    public static class CompressionHelper
    {
        public static byte[] InflateZlib(byte[] compressed, int expectedSize)
        {
            if (compressed == null || compressed.Length < 2)
            {
                throw new InvalidDataException("Buffer ZLIB invalido.");
            }

            // Pula os 2 bytes de cabecalho ZLIB (CMF + FLG)
            using (MemoryStream msIn = new MemoryStream(compressed, 2, compressed.Length - 2))
            using (DeflateStream deflate = new DeflateStream(msIn, CompressionMode.Decompress))
            using (MemoryStream msOut = new MemoryStream(expectedSize > 0 ? expectedSize : 4096))
            {
                deflate.CopyTo(msOut);
                byte[] result = msOut.ToArray();
                if (expectedSize > 0 && result.Length != expectedSize)
                {
                    byte[] sized = new byte[expectedSize];
                    Buffer.BlockCopy(result, 0, sized, 0, Math.Min(result.Length, expectedSize));
                    return sized;
                }
                return result;
            }
        }

        public static byte[] DeflateZlib(byte[] uncompressed)
        {
            if (uncompressed == null)
            {
                throw new ArgumentNullException(nameof(uncompressed));
            }

            using (MemoryStream msOut = new MemoryStream())
            {
                // Cabecalho padrao ZLIB (CMF=0x78, FLG=0x9C)
                msOut.WriteByte(0x78);
                msOut.WriteByte(0x9C);

                using (DeflateStream deflate = new DeflateStream(msOut, CompressionLevel.Optimal, true))
                {
                    deflate.Write(uncompressed, 0, uncompressed.Length);
                }

                uint adler = ComputeAdler32(uncompressed);
                msOut.WriteByte((byte)((adler >> 24) & 0xFF));
                msOut.WriteByte((byte)((adler >> 16) & 0xFF));
                msOut.WriteByte((byte)((adler >> 8) & 0xFF));
                msOut.WriteByte((byte)(adler & 0xFF));

                return msOut.ToArray();
            }
        }

        private static uint ComputeAdler32(byte[] data)
        {
            const uint modAdler = 65521;
            uint a = 1;
            uint b = 0;

            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % modAdler;
                b = (b + a) % modAdler;
            }

            return (b << 16) | a;
        }

        /// <summary>
        /// Descompressor de bloco LZ4 puro em C# (formato raw block usado pelo Skyrim SE).
        /// </summary>
        public static byte[] DecompressLz4(byte[] input, int uncompressedLength)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            byte[] output = new byte[uncompressedLength];
            int srcPos = 0;
            int dstPos = 0;
            int srcLen = input.Length;

            while (srcPos < srcLen)
            {
                int token = input[srcPos++];

                // Literais
                int literalLen = token >> 4;
                if (literalLen == 15)
                {
                    int add;
                    do
                    {
                        if (srcPos >= srcLen)
                        {
                            throw new InvalidDataException("Bloco LZ4 truncado ao ler comprimento literal.");
                        }
                        add = input[srcPos++];
                        literalLen += add;
                    }
                    while (add == 255);
                }

                if (literalLen > 0)
                {
                    if (srcPos + literalLen > srcLen || dstPos + literalLen > uncompressedLength)
                    {
                        throw new InvalidDataException("Bloco LZ4 corrompido (estouro de literais).");
                    }
                    Buffer.BlockCopy(input, srcPos, output, dstPos, literalLen);
                    srcPos += literalLen;
                    dstPos += literalLen;
                }

                if (srcPos >= srcLen)
                {
                    break;
                }

                // Offset da correspondencia (2 bytes little-endian)
                if (srcPos + 2 > srcLen)
                {
                    throw new InvalidDataException("Bloco LZ4 truncado ao ler offset.");
                }
                int offset = input[srcPos] | (input[srcPos + 1] << 8);
                srcPos += 2;

                if (offset <= 0 || offset > dstPos)
                {
                    throw new InvalidDataException("Offset LZ4 invalido: " + offset);
                }

                int matchLen = (token & 0x0F) + 4;
                if ((token & 0x0F) == 15)
                {
                    int add;
                    do
                    {
                        if (srcPos >= srcLen)
                        {
                            throw new InvalidDataException("Bloco LZ4 truncado ao ler comprimento de correspondencia.");
                        }
                        add = input[srcPos++];
                        matchLen += add;
                    }
                    while (add == 255);
                }

                if (dstPos + matchLen > uncompressedLength)
                {
                    throw new InvalidDataException("Bloco LZ4 excedeu o tamanho descompactado esperado.");
                }

                int matchPos = dstPos - offset;
                if (offset >= matchLen)
                {
                    Buffer.BlockCopy(output, matchPos, output, dstPos, matchLen);
                    dstPos += matchLen;
                }
                else
                {
                    for (int i = 0; i < matchLen; i++)
                    {
                        output[dstPos++] = output[matchPos++];
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Compressor de bloco LZ4 puro em C# (compativel com Skyrim SE).
        /// </summary>
        public static byte[] CompressLz4(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            int srcLen = input.Length;
            if (srcLen == 0)
            {
                return new byte[0];
            }

            const int hashBits = 16;
            const int hashSize = 1 << hashBits;
            const int minMatch = 4;
            const int maxDistance = 65535;
            const int mfLimit = 12;
            const int lastLiterals = 5;

            int[] hashTable = new int[hashSize];
            for (int i = 0; i < hashSize; i++)
            {
                hashTable[i] = -1;
            }

            using (MemoryStream ms = new MemoryStream(srcLen))
            {
                int anchor = 0;
                int pos = 0;
                int mflimitPos = srcLen - mfLimit;
                int matchLimit = srcLen - lastLiterals;

                if (srcLen >= mfLimit)
                {
                    hashTable[Hash4(input, 0)] = 0;
                    pos = 1;

                    while (pos < mflimitPos)
                    {
                        int matchPos = -1;
                        int step = 1;
                        int searchMatchNb = 1 << 6;

                        int curPos = pos;
                        while (curPos < mflimitPos)
                        {
                            int h = Hash4(input, curPos);
                            int candidate = hashTable[h];
                            hashTable[h] = curPos;

                            if (candidate >= 0 && (curPos - candidate) <= maxDistance && Equals4(input, candidate, curPos))
                            {
                                matchPos = candidate;
                                pos = curPos;
                                break;
                            }

                            int nextPos = curPos + step;
                            step = (searchMatchNb++) >> 6;
                            curPos = nextPos;
                        }

                        if (matchPos < 0)
                        {
                            break;
                        }

                        // Estender correspondencia para tras se possivel
                        while (pos > anchor && matchPos > 0 && input[pos - 1] == input[matchPos - 1])
                        {
                            pos--;
                            matchPos--;
                        }

                        int literalLen = pos - anchor;
                        int offset = pos - matchPos;

                        // Medir correspondencia para frente
                        int matchEnd = pos + minMatch;
                        int refEnd = matchPos + minMatch;
                        while (matchEnd < matchLimit && input[matchEnd] == input[refEnd])
                        {
                            matchEnd++;
                            refEnd++;
                        }
                        int matchLen = (matchEnd - pos) - minMatch;

                        // Escrever token
                        int tokenLit = literalLen >= 15 ? 15 : literalLen;
                        int tokenMatch = matchLen >= 15 ? 15 : matchLen;
                        ms.WriteByte((byte)((tokenLit << 4) | tokenMatch));

                        if (literalLen >= 15)
                        {
                            int rem = literalLen - 15;
                            while (rem >= 255)
                            {
                                ms.WriteByte(255);
                                rem -= 255;
                            }
                            ms.WriteByte((byte)rem);
                        }

                        if (literalLen > 0)
                        {
                            ms.Write(input, anchor, literalLen);
                        }

                        ms.WriteByte((byte)(offset & 0xFF));
                        ms.WriteByte((byte)((offset >> 8) & 0xFF));

                        if (matchLen >= 15)
                        {
                            int rem = matchLen - 15;
                            while (rem >= 255)
                            {
                                ms.WriteByte(255);
                                rem -= 255;
                            }
                            ms.WriteByte((byte)rem);
                        }

                        pos = matchEnd;
                        anchor = pos;

                        if (pos < mflimitPos)
                        {
                            hashTable[Hash4(input, pos - 2)] = pos - 2;
                        }
                    }
                }

                // Literais finais
                int lastLit = srcLen - anchor;
                int lastToken = lastLit >= 15 ? 15 : lastLit;
                ms.WriteByte((byte)(lastToken << 4));
                if (lastLit >= 15)
                {
                    int rem = lastLit - 15;
                    while (rem >= 255)
                    {
                        ms.WriteByte(255);
                        rem -= 255;
                    }
                    ms.WriteByte((byte)rem);
                }
                if (lastLit > 0)
                {
                    ms.Write(input, anchor, lastLit);
                }

                return ms.ToArray();
            }
        }

        private static int Hash4(byte[] data, int pos)
        {
            uint v = (uint)(data[pos]
                | (data[pos + 1] << 8)
                | (data[pos + 2] << 16)
                | (data[pos + 3] << 24));
            return (int)((v * 2654435761U) >> 16);
        }

        private static bool Equals4(byte[] data, int a, int b)
        {
            return data[a] == data[b]
                && data[a + 1] == data[b + 1]
                && data[a + 2] == data[b + 2]
                && data[a + 3] == data[b + 3];
        }
    }
}

