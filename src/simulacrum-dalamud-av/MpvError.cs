using Thinktecture;

namespace Simulacrum.AV;

internal sealed partial class MpvError : IEnum<int>
{
    public static readonly MpvError EventQueueFull = new(-1);
    public static readonly MpvError OutOfMemory = new(-2);
    public static readonly MpvError Uninitialized = new(-3);
    public static readonly MpvError InvalidParameter = new(-4);
    public static readonly MpvError OptionNotFound = new(-5);
    public static readonly MpvError OptionFormat = new(-6);
    public static readonly MpvError OptionError = new(-7);
    public static readonly MpvError PropertyNotFound = new(-8);
    public static readonly MpvError PropertyFormat = new(-9);
    public static readonly MpvError PropertyUnavailable = new(-10);
    public static readonly MpvError PropertyError = new(-11);
    public static readonly MpvError CommandError = new(-12);
    public static readonly MpvError LoadingFailed = new(-13);
    public static readonly MpvError AudioInitFailed = new(-14);
    public static readonly MpvError VideoInitFailed = new(-15);
    public static readonly MpvError NothingToPlay = new(-16);
    public static readonly MpvError UnknownFormat = new(-17);
    public static readonly MpvError Unsupported = new(-18);
    public static readonly MpvError NotImplemented = new(-19);
    public static readonly MpvError GenericError = new(-20);
}