using AOT;
using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xsolla.Core;

namespace PrimeGames.SDK.XSolla.Web
{
    [Provider(typeof(IPayments))]
    public class CrazyGamesXSollaPayments : CommonXSollaPayments
    {
        public CrazyGamesXSollaPayments(IData data) : base(data)
        {
            GetXSollaToken(
                (token) => {
                    Logger.CreateText(this, $"Successfully got XSolla token {token}");
                    XsollaToken.Create(token);
                    GetItems();
                },
                () => {
                    Logger.CreateError(this, "Failed to get XSolla token from CrazyGames");
                    GetItems();
                }
            );
        }

        [DllImport(Naming.InternalDll)]
        private static extern bool crazyGames_isLoggedIn();

        protected override bool IsLoggedIn()
        {
            return crazyGames_isLoggedIn();
        }

        [DllImport(Naming.InternalDll)]
        private static extern void crazyGames_invokeLogin(int senderId, DelegateVoid onSuccess, DelegateVoid onError);

        private record OnLoginSuccessInfo
        {
            public Action onSuccess;
        }

        private record OnLoginErrorInfo
        {
            public Action onError;
        }

        private static readonly Dictionary<int, OnLoginSuccessInfo> onLoginSuccessCallbacks = new();
        private static readonly Dictionary<int, OnLoginErrorInfo> onLoginErrorCallbacks = new();
        private static int nextLoginSenderId = 0;

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnLoginSuccess(int senderId)
        {
            if (onLoginSuccessCallbacks.TryGetValue(senderId, out OnLoginSuccessInfo onSuccessInfo))
            {
                onSuccessInfo.onSuccess?.Invoke();
                onLoginSuccessCallbacks.Remove(senderId);
            }
            else
            {
                throw new ArgumentException($"SenderId {senderId} not found in collection");
            }
        }

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnLoginError(int senderId)
        {
            if (onLoginErrorCallbacks.TryGetValue(senderId, out OnLoginErrorInfo onErrorInfo))
            {
                onErrorInfo.onError?.Invoke();
                onLoginErrorCallbacks.Remove(senderId);
            }
            else
            {
                throw new ArgumentException($"SenderId {senderId} not found in collection");
            }
        }

        protected override void InvokeLogin(Action onSuccess, Action onError)
        {
            int senderId = nextLoginSenderId++;
            onLoginSuccessCallbacks[senderId] = new OnLoginSuccessInfo { onSuccess = onSuccess };
            onLoginErrorCallbacks[senderId] = new OnLoginErrorInfo { onError = onError };
            crazyGames_invokeLogin(senderId, OnLoginSuccess, OnLoginError);
        }

        [DllImport(Naming.InternalDll)]
        private static extern void crazyGames_getXSollaToken(int senderId, DelegateString onSuccess, DelegateVoid onError);

        private record OnSuccessInfo
        {
            public Action<string> onSuccess;
        }
        private record OnErrorInfo
        {
            public Action onError;
        }

        private static readonly Dictionary<int, OnSuccessInfo> onTokenSuccessCallbacks = new();
        private static readonly Dictionary<int, OnErrorInfo> onTokenErrorCallbacks = new();
        private static int nextTokenSenderId = 0;

        [MonoPInvokeCallback(typeof(DelegateString))]
        private static void OnTokenSuccess(int senderId, string token)
        {
            if (onTokenSuccessCallbacks.TryGetValue(senderId, out OnSuccessInfo onSuccessInfo))
            {
                onSuccessInfo.onSuccess?.Invoke(token);
                onTokenSuccessCallbacks.Remove(senderId);
            }
            else
            {
                throw new ArgumentException($"SenderId {senderId} not found in collection");
            }
        }

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnTokenError(int senderId)
        {
            if (onTokenErrorCallbacks.TryGetValue(senderId, out OnErrorInfo onErrorInfo))
            {
                onErrorInfo.onError?.Invoke();
                onTokenErrorCallbacks.Remove(senderId);
            }
            else
            {
                throw new ArgumentException($"SenderId {senderId} not found in collection");
            }
        }

        private static void GetXSollaToken(Action<string> onSuccess, Action onError)
        {
            int senderId = nextTokenSenderId++;
            onTokenSuccessCallbacks[senderId] = new OnSuccessInfo { onSuccess = onSuccess };
            onTokenErrorCallbacks[senderId] = new OnErrorInfo { onError = onError };
            crazyGames_getXSollaToken(senderId, OnTokenSuccess, OnTokenError);
        }
    }
}