namespace SysBot.Base;

public class MultiRangeCompare<T> where T : IScreenshotComparer
{
    public readonly T[] Comparers;

    public MultiRangeCompare(params T[] comparers)
    {
        Comparers = comparers;
    }

    public bool Compare(SwitchScreenshot screen)
    {
        T[] comparers = Comparers;
        for (int i = 0; i < comparers.Length; i++)
        {
            T val = comparers[i];
            if (!val.Compare(screen))
            {
                return false;
            }
        }
        return true;
    }
}
