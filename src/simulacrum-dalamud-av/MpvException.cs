namespace Simulacrum.AV;

public class MpvException : Exception
{
    private MpvException(string message) : base(message)
    {
    }

    public static void ThrowMpvError(int error)
    {
        // Not using mpv_error_string here because it was unclear how to marshal the returned string correctly
        // Simple model of these errors: https://github.com/mpv-player/mpv/blob/665a47209869d7a0c4ea860b28910fcd6ca874c8/libmpv/client.h#L274-L380
        if (error >= 0) return;
        var mpvError = MpvError.Get(error);
        mpvError.Switch(
            MpvError.EventQueueFull, static () => throw new MpvException("The event ring buffer is full."),
            MpvError.OutOfMemory, static () => throw new MpvException("Memory allocation failed."),
            MpvError.Uninitialized, static () => throw new MpvException("The mpv core has not yet been configured or initialized."),
            MpvError.InvalidParameter, static () => throw new MpvException("Invalid parameter."),
            MpvError.OptionNotFound, static () => throw new MpvException("Trying to set an option that doesn't exist."),
            MpvError.OptionFormat, static () => throw new MpvException("Trying to set an option using an unsupported MPV_FORMAT."),
            MpvError.OptionError, static () => throw new MpvException("Setting the option failed."),
            MpvError.PropertyNotFound, static () => throw new MpvException("The accessed property doesn't exist."),
            MpvError.PropertyFormat, static () => throw new MpvException("Trying to set or get a property using an unsupported MPV_FORMAT."),
            MpvError.PropertyUnavailable, static () => throw new MpvException("The property exists, but is not available."),
            MpvError.PropertyError, static () => throw new MpvException("Error setting or getting a property."),
            MpvError.CommandError, static () => throw new MpvException("Command failed."),
            MpvError.LoadingFailed, static () => throw new MpvException("Loading failed."),
            MpvError.AudioInitFailed, static () => throw new MpvException("Initializing the audio output failed."),
            MpvError.VideoInitFailed, static () => throw new MpvException("Initializing the video output failed."),
            MpvError.NothingToPlay, static () => throw new MpvException("There was no audio or video data to play."),
            MpvError.UnknownFormat, static () => throw new MpvException("When trying to load the file, the file format could not be determined, or the file was too broken to open it."),
            MpvError.Unsupported, static () => throw new MpvException("The system does not support the requested operation."),
            MpvError.NotImplemented, static () => throw new MpvException("Not yet implemented."),
            MpvError.GenericError, static () => throw new MpvException("An error occurred."));
    }
}