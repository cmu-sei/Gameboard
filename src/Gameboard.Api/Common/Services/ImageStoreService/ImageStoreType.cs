namespace Gameboard.Api.Common.Services;

public class ImageStoreType
{
    private readonly string _value;
    private ImageStoreType(string value) { _value = value; }

    public static ImageStoreType Game { get => new("game"); }
    public static ImageStoreType PracticeChallengeGroup { get => new("practice-challenge-group"); }
    public static ImageStoreType Sponsor { get => new("sponsor"); }

    public override string ToString()
    {
        return _value;
    }
}
