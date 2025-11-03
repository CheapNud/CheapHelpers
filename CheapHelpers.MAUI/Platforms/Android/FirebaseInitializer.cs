using Android.App;
using Firebase;
using System.Diagnostics;

namespace CheapHelpers.MAUI.Platforms.Android;

/// <summary>
/// Helper class for safely initializing Firebase in your Android application
/// </summary>
/// <remarks>
/// To use this in your MAUI app, call from your MainApplication.OnCreate():
/// <code>
/// [Application]
/// public class MainApplication : MauiApplication
/// {
///     public override void OnCreate()
///     {
///         base.OnCreate();
///         FirebaseInitializer.Initialize(this);
///     }
/// }
/// </code>
/// </remarks>
public static class FirebaseInitializer
{
    private static bool _isInitialized = false;
    private static FirebaseApp? _firebaseAppInstance;

    /// <summary>
    /// Whether Firebase was successfully initialized
    /// </summary>
    public static bool IsFirebaseAvailable { get; private set; } = false;

    /// <summary>
    /// The Firebase app instance (null if initialization failed)
    /// </summary>
    public static FirebaseApp? FirebaseAppInstance => _firebaseAppInstance;

    /// <summary>
    /// Initialize Firebase with detailed error handling and diagnostics
    /// </summary>
    public static void Initialize(global::Android.App.Application application)
    {
        if (_isInitialized)
        {
            Debug.WriteLine("Firebase already initialized");
            return;
        }

        _isInitialized = true;

        try
        {
            // Check if google-services.json exists and is valid
            _firebaseAppInstance = FirebaseApp.InitializeApp(application);

            if (_firebaseAppInstance != null)
            {
                IsFirebaseAvailable = true;
                Debug.WriteLine("Firebase initialized successfully");
            }
            else
            {
                IsFirebaseAvailable = false;
                Debug.WriteLine("Firebase app initialization returned null");
                Debug.WriteLine("This usually means google-services.json is missing or invalid");
            }
        }
        catch (Java.Lang.IllegalStateException ex) when (ex.Message?.Contains("Default FirebaseApp is not initialized") == true)
        {
            IsFirebaseAvailable = false;
            Debug.WriteLine($"Firebase not properly configured: {ex.Message}");
            Debug.WriteLine("Ensure google-services.json is in the Android project and Build Action is set to GoogleServicesJson");
        }
        catch (Java.Lang.IllegalArgumentException ex) when (ex.Message?.Contains("given String is empty or null") == true)
        {
            IsFirebaseAvailable = false;
            Debug.WriteLine($"Firebase configuration has empty/null values: {ex.Message}");
            Debug.WriteLine("Check google-services.json for missing or empty required fields");
        }
        catch (Exception ex)
        {
            IsFirebaseAvailable = false;
            Debug.WriteLine($"Firebase initialization failed: {ex.GetType().Name}");
            Debug.WriteLine($"Error message: {ex.Message}");

            // Log additional diagnostic information
            LogFirebaseDiagnostics(application);
        }
    }

    private static void LogFirebaseDiagnostics(global::Android.App.Application application)
    {
        try
        {
            Debug.WriteLine("Firebase Diagnostics:");
            Debug.WriteLine($"Package Name: {application.PackageName}");

            // Check if google-services.json resource exists
            var resourceId = application.Resources?.GetIdentifier("google_services_json", "raw", application.PackageName);
            Debug.WriteLine($"google-services.json resource: {(resourceId > 0 ? "Found" : "Not Found")}");

            // Check Google Play Services availability
            var availability = global::Android.Gms.Common.GoogleApiAvailability.Instance;
            var playServicesResult = availability?.IsGooglePlayServicesAvailable(application);
            Debug.WriteLine($"Google Play Services: {(playServicesResult == global::Android.Gms.Common.ConnectionResult.Success ? "Available" : "Not Available")}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to log Firebase diagnostics: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the current Firebase initialization status
    /// </summary>
    public static (bool IsAvailable, string Status, FirebaseApp? App) GetStatus()
    {
        var status = IsFirebaseAvailable switch
        {
            true when _firebaseAppInstance != null => "Initialized and ready",
            true when _firebaseAppInstance == null => "Available but app instance is null",
            false => "Not available",
            _ => "Initialization failed"
        };

        return (IsFirebaseAvailable, status, _firebaseAppInstance);
    }
}
