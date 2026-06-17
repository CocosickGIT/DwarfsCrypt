using TMPro;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Windows
{
    public class ErrorWindow : WindowBase
    {
        [SerializeField] private TMP_Text _messageText;

        public override WindowType Type => WindowType.Error;

        public void Open(string message)
        {
            if (_messageText != null)
                _messageText.text = message;
            Open();
        }
    }
}
