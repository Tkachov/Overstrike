// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalizationTool.Helpers {
    public class UndoRedoManager {
        private Stack<Action> undoStack = new Stack<Action>();
        private Stack<Action> redoStack = new Stack<Action>();
        private int undoStackLimit = 100;
        private int cleanState = 0;

        public void AddChange(Action change) {
            // Add the change to the Undo stack
            if (undoStack.Count > undoStackLimit) {
                undoStack.Reverse();
                undoStack.Pop();
                undoStack.Reverse();
                cleanState = -1;
            }
            undoStack.Push(change);
            // Clear the Redo stack since a new change has occurred
            if (undoStack.Count <= cleanState) {
                cleanState = -1;
            }
            redoStack.Clear();
        }

        public bool IsCleanState => (cleanState == undoStack.Count);

        public void SaveState() {
            cleanState = undoStack.Count;
        }

        public void InitState() {
            cleanState = 0;
        }

        public Stack<Action> UndoStack => undoStack;
        public Stack<Action> RedoStack => redoStack;

        public Action Undo() {
            if (undoStack.Count > 0) {
                Action change = undoStack.Pop();
                change = change.Reverse();
                redoStack.Push(change);
                return change;
            }
            return null;
        }

        public Action Redo() {
            if (redoStack.Count > 0) {
                Action change = redoStack.Pop();
                change = change.Reverse();
                undoStack.Push(change);
                return change;
            }
            return null;
        }
    }

    public class Action {
        public LocalizationEntry OldEntry { get; set; }
        public LocalizationEntry Entry { get; set; }
        public ActionType Type { get; set; }

        public Action Reverse() {
            Action reversedChange = new Action { Entry = Entry, Type = Type, OldEntry = OldEntry };

            switch (Type) {
                case ActionType.Edit:
                    reversedChange.OldEntry = new LocalizationEntry(Entry.Key, Entry.Value, Entry.Flags);
                    reversedChange.Entry = new LocalizationEntry(OldEntry.Key, OldEntry.Value, OldEntry.Flags);
                    break;
                case ActionType.Add:
                    reversedChange.Entry = new LocalizationEntry(Entry.Key, Entry.Value, Entry.Flags);
                    reversedChange.Type = ActionType.Delete;
                    break;
                case ActionType.Delete:
                    reversedChange.Entry = new LocalizationEntry(Entry.Key, Entry.Value, Entry.Flags);
                    reversedChange.Type = ActionType.Add;
                    break;
                default:
                    throw new NotSupportedException("Unsupported ActionType");
            }
            return reversedChange;
        }
    }

    public enum ActionType {
        Edit,
        Add,
        Delete
    }
}
