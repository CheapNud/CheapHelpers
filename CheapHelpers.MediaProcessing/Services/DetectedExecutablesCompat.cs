namespace CheapHelpers.MediaProcessing.Services;

/// <summary>
/// Backward compatibility alias for DetectedExecutables.
/// Use <see cref="Models.DetectedExecutables"/> for new code.
/// </summary>
/// <remarks>
/// This class exists to maintain backward compatibility after DetectedExecutables
/// was moved from Services to Models namespace for cross-platform support.
/// </remarks>
[Obsolete("Use CheapHelpers.MediaProcessing.Models.DetectedExecutables instead. This alias will be removed in version 2.0.")]
public class DetectedExecutables : Models.DetectedExecutables { }
