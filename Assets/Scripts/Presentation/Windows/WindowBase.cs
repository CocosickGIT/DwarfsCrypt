using UnityEngine;

namespace DwarfsCrypt.Presentation.Windows
{
    public abstract class WindowBase : MonoBehaviour
    {
        public abstract WindowType Type { get; }
        public bool IsOpen { get; private set; }

        public virtual void Open()
        {
            IsOpen = true;
            gameObject.SetActive(true);
            OnOpen();
        }

        public virtual void Close()
        {
            IsOpen = false;
            gameObject.SetActive(false);
            OnClose();
        }

        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
    }
}
