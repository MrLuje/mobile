﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Text;
using Android.Text.Method;
using Android.Views.Autofill;
using Android.Webkit;
using Android.Widget;
using Bit.App.Abstractions;
using Bit.App.Resources;
using Bit.Core;
using Bit.Core.Abstractions;
using Bit.Core.Enums;
using Plugin.CurrentActivity;

namespace Bit.Droid.Services
{
    public class DeviceActionService : IDeviceActionService
    {
        private readonly IStorageService _storageService;
        private readonly IMessagingService _messagingService;
        private readonly IBroadcasterService _broadcasterService;
        private ProgressDialog _progressDialog;
        private bool _cameraPermissionsDenied;
        private Toast _toast;

        public DeviceActionService(
            IStorageService storageService,
            IMessagingService messagingService,
            IBroadcasterService broadcasterService)
        {
            _storageService = storageService;
            _messagingService = messagingService;
            _broadcasterService = broadcasterService;

            _broadcasterService.Subscribe(nameof(DeviceActionService), (message) =>
            {
                if(message.Command == "selectFileCameraPermissionDenied")
                {
                    _cameraPermissionsDenied = true;
                }
            });
        }

        public DeviceType DeviceType => DeviceType.Android;

        public void Toast(string text, bool longDuration = false)
        {
            if(_toast != null)
            {
                _toast.Cancel();
                _toast.Dispose();
                _toast = null;
            }
            _toast = Android.Widget.Toast.MakeText(CrossCurrentActivity.Current.Activity, text,
                longDuration ? ToastLength.Long : ToastLength.Short);
            _toast.Show();
        }

        public bool LaunchApp(string appName)
        {
            var activity = CrossCurrentActivity.Current.Activity;
            appName = appName.Replace("androidapp://", string.Empty);
            var launchIntent = activity.PackageManager.GetLaunchIntentForPackage(appName);
            if(launchIntent != null)
            {
                activity.StartActivity(launchIntent);
            }
            return launchIntent != null;
        }

        public async Task ShowLoadingAsync(string text)
        {
            if(_progressDialog != null)
            {
                await HideLoadingAsync();
            }
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            _progressDialog = new ProgressDialog(activity);
            _progressDialog.SetMessage(text);
            _progressDialog.SetCancelable(false);
            _progressDialog.Show();
        }

        public Task HideLoadingAsync()
        {
            if(_progressDialog != null)
            {
                _progressDialog.Dismiss();
                _progressDialog.Dispose();
                _progressDialog = null;
            }
            return Task.FromResult(0);
        }

        public bool OpenFile(byte[] fileData, string id, string fileName)
        {
            if(!CanOpenFile(fileName))
            {
                return false;
            }
            var extension = MimeTypeMap.GetFileExtensionFromUrl(fileName.Replace(' ', '_').ToLower());
            if(extension == null)
            {
                return false;
            }
            var mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            if(mimeType == null)
            {
                return false;
            }

            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            var cachePath = activity.CacheDir;
            var filePath = Path.Combine(cachePath.Path, fileName);
            File.WriteAllBytes(filePath, fileData);
            var file = new Java.IO.File(cachePath, fileName);
            if(!file.IsFile)
            {
                return false;
            }

            try
            {
                var intent = new Intent(Intent.ActionView);
                var uri = FileProvider.GetUriForFile(activity.ApplicationContext,
                    "com.x8bit.bitwarden.fileprovider", file);
                intent.SetDataAndType(uri, mimeType);
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);
                activity.StartActivity(intent);
                return true;
            }
            catch { }
            return false;
        }

        public bool CanOpenFile(string fileName)
        {
            var extension = MimeTypeMap.GetFileExtensionFromUrl(fileName.Replace(' ', '_').ToLower());
            if(extension == null)
            {
                return false;
            }
            var mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            if(mimeType == null)
            {
                return false;
            }
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            var intent = new Intent(Intent.ActionView);
            intent.SetType(mimeType);
            var activities = activity.PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return (activities?.Count ?? 0) > 0;
        }

        public async Task ClearCacheAsync()
        {
            try
            {
                DeleteDir(CrossCurrentActivity.Current.Activity.CacheDir);
                await _storageService.SaveAsync(Constants.LastFileCacheClearKey, DateTime.UtcNow);
            }
            catch(Exception) { }
        }

        public Task SelectFileAsync()
        {
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            var hasStorageWritePermission = !_cameraPermissionsDenied && HasPermission(Manifest.Permission.WriteExternalStorage);
            var additionalIntents = new List<IParcelable>();
            if(activity.PackageManager.HasSystemFeature(PackageManager.FeatureCamera))
            {
                var hasCameraPermission = !_cameraPermissionsDenied && HasPermission(Manifest.Permission.Camera);
                if(!_cameraPermissionsDenied && !hasStorageWritePermission)
                {
                    AskPermission(Manifest.Permission.WriteExternalStorage);
                    return Task.FromResult(0);
                }
                if(!_cameraPermissionsDenied && !hasCameraPermission)
                {
                    AskPermission(Manifest.Permission.Camera);
                    return Task.FromResult(0);
                }
                if(!_cameraPermissionsDenied && hasCameraPermission && hasStorageWritePermission)
                {
                    try
                    {
                        var root = new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory, "bitwarden");
                        var file = new Java.IO.File(root, "temp_camera_photo.jpg");
                        if(!file.Exists())
                        {
                            file.ParentFile.Mkdirs();
                            file.CreateNewFile();
                        }
                        var outputFileUri = Android.Net.Uri.FromFile(file);
                        additionalIntents.AddRange(GetCameraIntents(outputFileUri));
                    }
                    catch(Java.IO.IOException) { }
                }
            }

            var docIntent = new Intent(Intent.ActionOpenDocument);
            docIntent.AddCategory(Intent.CategoryOpenable);
            docIntent.SetType("*/*");
            var chooserIntent = Intent.CreateChooser(docIntent, AppResources.FileSource);
            if(additionalIntents.Count > 0)
            {
                chooserIntent.PutExtra(Intent.ExtraInitialIntents, additionalIntents.ToArray());
            }
            activity.StartActivityForResult(chooserIntent, Constants.SelectFileRequestCode);
            return Task.FromResult(0);
        }

        public Task<string> DisplayPromptAync(string title = null, string description = null,
            string text = null, string okButtonText = null, string cancelButtonText = null,
            bool numericKeyboard = false)
        {
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            if(activity == null)
            {
                return Task.FromResult<string>(null);
            }

            var alertBuilder = new AlertDialog.Builder(activity);
            alertBuilder.SetTitle(title);
            alertBuilder.SetMessage(description);
            var input = new EditText(activity)
            {
                InputType = InputTypes.ClassText
            };
            if(text == null)
            {
                text = string.Empty;
            }
            if(numericKeyboard)
            {
                input.InputType = InputTypes.ClassNumber | InputTypes.NumberFlagDecimal | InputTypes.NumberFlagSigned;
#pragma warning disable CS0618 // Type or member is obsolete
                input.KeyListener = DigitsKeyListener.GetInstance(false, false);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            input.Text = text;
            input.SetSelection(text.Length);
            var container = new FrameLayout(activity);
            var lp = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.MatchParent);
            lp.SetMargins(25, 0, 25, 0);
            input.LayoutParameters = lp;
            container.AddView(input);
            alertBuilder.SetView(container);

            okButtonText = okButtonText ?? AppResources.Ok;
            cancelButtonText = cancelButtonText ?? AppResources.Cancel;
            var result = new TaskCompletionSource<string>();
            alertBuilder.SetPositiveButton(okButtonText,
                (sender, args) => result.TrySetResult(input.Text ?? string.Empty));
            alertBuilder.SetNegativeButton(cancelButtonText, (sender, args) => result.TrySetResult(null));

            var alert = alertBuilder.Create();
            alert.Window.SetSoftInputMode(Android.Views.SoftInput.StateVisible);
            alert.Show();
            return result.Task;
        }

        public void RateApp()
        {
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            try
            {
                var rateIntent = RateIntentForUrl("market://details", activity);
                activity.StartActivity(rateIntent);
            }
            catch(ActivityNotFoundException)
            {
                var rateIntent = RateIntentForUrl("https://play.google.com/store/apps/details", activity);
                activity.StartActivity(rateIntent);
            }
        }

        public bool SupportsFaceId()
        {
            return false;
        }

        public bool SupportsNfc()
        {
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            var manager = activity.GetSystemService(Context.NfcService) as NfcManager;
            return manager.DefaultAdapter?.IsEnabled ?? false;
        }

        public bool SupportsCamera()
        {
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            return activity.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);
        }

        public bool SupportsAutofillService()
        {
            if(Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return false;
            }
            var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
            var type = Java.Lang.Class.FromType(typeof(AutofillManager));
            var manager = activity.GetSystemService(type) as AutofillManager;
            return manager.IsAutofillSupported;
        }

        private bool DeleteDir(Java.IO.File dir)
        {
            if(dir != null && dir.IsDirectory)
            {
                var children = dir.List();
                for(int i = 0; i < children.Length; i++)
                {
                    var success = DeleteDir(new Java.IO.File(dir, children[i]));
                    if(!success)
                    {
                        return false;
                    }
                }
                return dir.Delete();
            }
            else if(dir != null && dir.IsFile)
            {
                return dir.Delete();
            }
            else
            {
                return false;
            }
        }

        private bool HasPermission(string permission)
        {
            return ContextCompat.CheckSelfPermission(
                CrossCurrentActivity.Current.Activity, permission) == Permission.Granted;
        }

        private void AskPermission(string permission)
        {
            ActivityCompat.RequestPermissions(CrossCurrentActivity.Current.Activity, new string[] { permission },
                Constants.SelectFilePermissionRequestCode);
        }

        private List<IParcelable> GetCameraIntents(Android.Net.Uri outputUri)
        {
            var intents = new List<IParcelable>();
            var pm = CrossCurrentActivity.Current.Activity.PackageManager;
            var captureIntent = new Intent(MediaStore.ActionImageCapture);
            var listCam = pm.QueryIntentActivities(captureIntent, 0);
            foreach(var res in listCam)
            {
                var packageName = res.ActivityInfo.PackageName;
                var intent = new Intent(captureIntent);
                intent.SetComponent(new ComponentName(packageName, res.ActivityInfo.Name));
                intent.SetPackage(packageName);
                intent.PutExtra(MediaStore.ExtraOutput, outputUri);
                intents.Add(intent);
            }
            return intents;
        }

        private Intent RateIntentForUrl(string url, Activity activity)
        {
            var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse($"{url}?id={activity.PackageName}"));
            var flags = ActivityFlags.NoHistory | ActivityFlags.MultipleTask;
            if((int)Build.VERSION.SdkInt >= 21)
            {
                flags |= ActivityFlags.NewDocument;
            }
            else
            {
                // noinspection deprecation
                flags |= ActivityFlags.ClearWhenTaskReset;
            }
            intent.AddFlags(flags);
            return intent;
        }
    }
}