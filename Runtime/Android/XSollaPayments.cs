using PrimeGames.SDK.Common;
using System;
using Xsolla.Auth;
using Xsolla.Core;

namespace PrimeGames.SDK.XSolla.Android
{
    [Provider(typeof(IPayments))]
    public class XSollaPayments : CommonXSollaPayments
    {
        public XSollaPayments(IData data) : base(data)
        {
            GetItems();
        }

        protected override void InvokeLogin(Action onSuccess, Action onError)
        {
            if (XsollaAuth.IsUserAuthenticated())
            {
                onSuccess?.Invoke();
                return;
            }

            XsollaAuth.AuthViaDeviceID(
                onSuccess: () => onSuccess?.Invoke(),
                onError: (error) =>
                {
                    Logger.CreateError(this, $"Failed to authenticate via device ID: {error}");
                    onError?.Invoke();
                }
            );
        }

        protected override bool IsLoggedIn()
        {
            return XsollaAuth.IsUserAuthenticated();
        }
    }
}