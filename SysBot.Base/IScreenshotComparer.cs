namespace SysBot.Base;

public interface IScreenshotComparer
{
    bool Compare(SwitchScreenshot screen);

    int CompareRange(SwitchScreenshot screen);
}
