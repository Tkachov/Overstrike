// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

namespace LocalizationTool {
    public class LocalizationEntry {
        private string _key;
        private string _value;
        private uint _flags;
        private bool _changed;
        private string _originalKey;
        private string _originalValue;
        private uint _originalFlags;

        public LocalizationEntry(string key, string value, uint flags, bool changed = false) {
            _key = key;
            _value = value;
            _flags = flags;
            _originalKey = key;
            _originalValue = value;
            _originalFlags = flags;
            _changed = changed;
        }

        public string ValueWithLineBreaks {
            get {
                string result = "";
                int currentLineCount = 0;

                foreach (var c in Value) {
                    if (currentLineCount >= 80 && c == ' ') {
                        result += "\n";
                        currentLineCount = 0;
                        continue;
                    }

                    result += c;
                    currentLineCount++;
                }
                return result;
            }
        }

        public bool IsChanged => _changed;

        public void Clear() {
            _changed = false;
        }

        private void ContainChanges() {
            if ((_key != _originalKey) || (_value != _originalValue) || (_flags != _originalFlags)) {
                _changed = true;
            }
            else {
                _changed = false;
            }
        }

        public string Key {
            get { return _key; }
            set {
                if (_key != value) {
                    _key = value;
                    ContainChanges();
                }
            }
        }

        public string Value {
            get { return _value; }
            set {
                if (_value != value) {
                    _value = value;
                    ContainChanges();
                }
            }
        }

        public uint Flags {
            get { return _flags; }
            set {
                if (_flags != value) {
                    _flags = value;
                    ContainChanges();

                }
            }
        }

        public LocalizationEntry DeepCopy() {
            LocalizationEntry copy = new LocalizationEntry(_key, _value, _flags, _changed) {
                _originalKey = _originalKey,
                _originalValue = _originalValue,
                _originalFlags = _originalFlags
            };
            return copy;
        }
    }
}