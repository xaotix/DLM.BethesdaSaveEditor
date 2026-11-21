using System;
using System.Collections.Generic;

namespace DLM.BethesdaSaveEditor.Core
{
    public sealed class UndoAction
    {
        public string Description { get; }
        public Action Undo { get; }
        public Action Redo { get; }

        public UndoAction(string description, Action undo, Action redo)
        {
            Description = description ?? "Edit";
            Undo = undo ?? throw new ArgumentNullException(nameof(undo));
            Redo = redo ?? throw new ArgumentNullException(nameof(redo));
        }
    }

    public sealed class UndoRedoManager : ObservableBase
    {
        private readonly Stack<UndoAction> _undoStack = new Stack<UndoAction>();
        private readonly Stack<UndoAction> _redoStack = new Stack<UndoAction>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public string NextUndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : string.Empty;
        public string NextRedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : string.Empty;

        public string FooterSummary
        {
            get
            {
                string uLabel = LocalizationService.Instance["FooterUndo"];
                string rLabel = LocalizationService.Instance["FooterRedo"];
                string uDesc = _undoStack.Count > 0 ? (" [" + _undoStack.Peek().Description + "]") : string.Empty;
                return string.Format("{0}: {1}{2} | {3}: {4}", uLabel, _undoStack.Count, uDesc, rLabel, _redoStack.Count);
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            NotifyAll();
        }

        public void Record(string description, Action undo, Action redo)
        {
            _undoStack.Push(new UndoAction(description, undo, redo));
            _redoStack.Clear();
            NotifyAll();
        }

        public string PerformUndo()
        {
            if (_undoStack.Count == 0) return null;
            UndoAction action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
            NotifyAll();
            return action.Description;
        }

        public string PerformRedo()
        {
            if (_redoStack.Count == 0) return null;
            UndoAction action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
            NotifyAll();
            return action.Description;
        }

        public void NotifyAll()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoCount));
            OnPropertyChanged(nameof(RedoCount));
            OnPropertyChanged(nameof(NextUndoDescription));
            OnPropertyChanged(nameof(NextRedoDescription));
            OnPropertyChanged(nameof(FooterSummary));
        }
    }
}

