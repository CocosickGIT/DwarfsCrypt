using DwarfsCrypt.Presentation.Windows;

namespace Presentation.WindowService
{
    public interface IWindowService
    {
        void Open(WindowType type);
        void Close(WindowType type);
        void CloseAll();
        bool IsOpen(WindowType type);
        T GetWindow<T>(WindowType type) where T : WindowBase;
    }
}
