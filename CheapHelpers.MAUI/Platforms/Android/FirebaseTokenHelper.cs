#if ANDROID
using Android.App;
using CheapHelpers.Blazor.Hybrid.Abstractions;
using Firebase.Messaging;

namespace CheapHelpers.MAUI.Platforms.Android;

/// <summary>
/// Helper utility for safely retrieving Firebase Cloud Messaging (FCM) tokens.
/// This helper is separate from system bar configuration to maintain proper separation of concerns.
/// </summary>
/// <remarks>
/// <para>
/// This helper provides safe Firebase token retrieval with proper null checks and error handling.
/// It checks Firebase availability and notification support before requesting tokens.
/// </para>
/// <para>
/// <b>Requirements:</b>
/// <list type="bullet">
/// <item>Firebase must be initialized in your application</item>
/// <item>Google Play Services must be available</item>
/// <item>Activity must implement Android.Gms.Tasks.IOnSuccessListener</item>
/// </list>
/// </para>
/// </remarks>
public static class FirebaseTokenHelper
{
    /// <summary>
    /// Safely retrieves the Firebase Cloud Messaging (FCM) token with proper null checks and error handling.
    /// This method checks Firebase availability and notification support before requesting the token.
    /// </summary>
    /// <param name="activity">The activity context (must implement IOnSuccessListener)</param>
    /// <param name="deviceService">The device installation service that will receive the token</param>
    /// <param name="firebaseAvailabilityCheck">Optional function to check if Firebase is available. If null, assumes Firebase is available.</param>
    /// <exception cref="ArgumentNullException">Thrown when activity or deviceService is null</exception>
    /// <remarks>
    /// <para>
    /// This method performs safe Firebase token retrieval with the following checks:
    /// <list type="number">
    /// <item>Validates Firebase availability (if check function provided)</item>
    /// <item>Checks device notification support</item>
    /// <item>Validates activity implements IOnSuccessListener</item>
    /// <item>Requests FCM token asynchronously</item>
    /// <item>Handles all exceptions gracefully</item>
    /// </list>
    /// </para>
    /// <para>
    /// The token is retrieved asynchronously and will be set via the OnSuccess callback.
    /// The activity must implement Android.Gms.Tasks.IOnSuccessListener to receive the token.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using CheapHelpers.MAUI.Helpers;
    /// using CheapHelpers.Blazor.Hybrid.Abstractions;
    ///
    /// public class MainActivity : MauiAppCompatActivity, Android.Gms.Tasks.IOnSuccessListener
    /// {
    ///     private IDeviceInstallationService? _deviceService;
    ///
    ///     protected override void OnCreate(Bundle? savedInstanceState)
    ///     {
    ///         base.OnCreate(savedInstanceState);
    ///
    ///         _deviceService = IPlatformApplication.Current?.Services?
    ///             .GetService&lt;IDeviceInstallationService&gt;();
    ///
    ///         // Safely get Firebase token
    ///         FirebaseTokenHelper.GetFirebaseTokenSafely(
    ///             this,
    ///             _deviceService,
    ///             () => MainApplication.IsFirebaseAvailable
    ///         );
    ///     }
    ///
    ///     public void OnSuccess(Java.Lang.Object result)
    ///     {
    ///         var token = result?.ToString() ?? "unknown-token";
    ///         (_deviceService as CheapHelpers.MAUI.Platforms.Android.DeviceInstallationService)?.SetToken(token);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static void GetFirebaseTokenSafely(
        Activity activity,
        IDeviceInstallationService deviceService,
        Func<bool>? firebaseAvailabilityCheck = null)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));

        if (deviceService == null)
            throw new ArgumentNullException(nameof(deviceService));

        try
        {
            // Check Firebase availability if provided
            if (firebaseAvailabilityCheck != null && !firebaseAvailabilityCheck())
            {
                System.Diagnostics.Debug.WriteLine("FirebaseTokenHelper: Firebase is not available, skipping token retrieval");
                return;
            }

            // Check if device supports notifications
            if (!deviceService.NotificationsSupported)
            {
                System.Diagnostics.Debug.WriteLine("FirebaseTokenHelper: Notifications not supported on this device, skipping token retrieval");
                return;
            }

            // Check if activity implements IOnSuccessListener
            if (activity is not global::Android.Gms.Tasks.IOnSuccessListener listener)
            {
                System.Diagnostics.Debug.WriteLine("FirebaseTokenHelper: Activity does not implement IOnSuccessListener, cannot retrieve token");
                return;
            }

            // Now safe to call Firebase - we know it's initialized
            FirebaseMessaging.Instance.GetToken().AddOnSuccessListener(listener);

            System.Diagnostics.Debug.WriteLine("FirebaseTokenHelper: Firebase token request initiated");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirebaseTokenHelper: Failed to get Firebase token: {ex.Message}");
        }
    }

    /// <summary>
    /// Requests a new Firebase token, forcing a refresh even if one already exists.
    /// </summary>
    /// <param name="activity">The activity context (must implement IOnSuccessListener)</param>
    /// <param name="firebaseAvailabilityCheck">Optional function to check if Firebase is available</param>
    /// <exception cref="ArgumentNullException">Thrown when activity is null</exception>
    /// <remarks>
    /// Use this method when you need to force a token refresh, such as after a user logout/login.
    /// </remarks>
    public static void RefreshFirebaseToken(
        Activity activity,
        Func<bool>? firebaseAvailabilityCheck = null)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));

        try
        {
            // Check Firebase availability if provided
            if (firebaseAvailabilityCheck != null && !firebaseAvailabilityCheck())
            {
                System.Diagnostics.Debug.WriteLine("FirebaseTokenHelper: Firebase is not available, skipping token refresh");
                return;
            }

            // Check if activity implements IOnSuccessListener
            if (activity is not global::Android.Gms.Tasks.IOnSuccessListener listener)
            {
                System.Diagnostics.Debug.WriteLine("FirebaseTokenHelper: Activity does not implement IOnSuccessListener, cannot refresh token");
                return;
            }

            // Delete the current token and get a new one
            FirebaseMessaging.Instance.DeleteToken();
            FirebaseMessaging.Instance.GetToken().AddOnSuccessListener(listener);

            System.Diagnostics.Debug.WriteLine("FirebaseTokenHelper: Firebase token refresh initiated");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirebaseTokenHelper: Failed to refresh Firebase token: {ex.Message}");
        }
    }
}
#endif
