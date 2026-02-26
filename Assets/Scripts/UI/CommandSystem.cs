using System.Collections.Generic;
using UnityEngine;

namespace PocoRender.UI
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    public class CommandHistory
    {
        private Stack<ICommand> undoStack = new Stack<ICommand>();
        private Stack<ICommand> redoStack = new Stack<ICommand>();

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
        }

        public void AddToHistory(ICommand command)
        {
            undoStack.Push(command);
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                ICommand command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                ICommand command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }
        }
    }

    public class MoveCommand : ICommand
    {
        private RectTransform target;
        private Vector2 oldPos;
        private Vector2 newPos;
        private System.Action onComplete;

        public MoveCommand(RectTransform target, Vector2 oldPos, Vector2 newPos, System.Action onComplete = null)
        {
            this.target = target;
            this.oldPos = oldPos;
            this.newPos = newPos;
            this.onComplete = onComplete;
        }

        public void Execute()
        {
            if (target == null) return;
            target.anchoredPosition = newPos;
            onComplete?.Invoke();
        }

        public void Undo()
        {
            if (target == null) return;
            target.anchoredPosition = oldPos;
            onComplete?.Invoke();
        }
    }

    public class RotateCommand : ICommand
    {
        private RectTransform target;
        private Quaternion oldRot;
        private Quaternion newRot;
        private System.Action onComplete;

        public RotateCommand(RectTransform target, Quaternion oldRot, Quaternion newRot, System.Action onComplete = null)
        {
            this.target = target;
            this.oldRot = oldRot;
            this.newRot = newRot;
            this.onComplete = onComplete;
        }

        public void Execute()
        {
            if (target == null) return;
            target.rotation = newRot;
            onComplete?.Invoke();
        }

        public void Undo()
        {
            if (target == null) return;
            target.rotation = oldRot;
            onComplete?.Invoke();
        }
    }

    public class AddObjectCommand : ICommand
    {
        private GameObject prefab;
        private GameObject instance;
        private Transform parent;

        public AddObjectCommand(GameObject obj, Transform parent)
        {
            this.instance = obj;
            this.parent = parent;
        }

        public void Execute()
        {
            if (instance != null) instance.SetActive(true);
        }

        public void Undo()
        {
            if (instance != null) instance.SetActive(false);
        }
    }

    public class DeleteObjectCommand : ICommand
    {
        private GameObject target;
        private System.Action onUndo;

        public DeleteObjectCommand(GameObject target, System.Action onUndo = null)
        {
            this.target = target;
            this.onUndo = onUndo;
        }

        public void Execute()
        {
            if (target != null) target.SetActive(false);
        }

        public void Undo()
        {
            if (target != null)
            {
                target.SetActive(true);
                onUndo?.Invoke();
            }
        }
    }
}


