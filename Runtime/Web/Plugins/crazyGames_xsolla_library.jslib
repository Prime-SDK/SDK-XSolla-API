const crazyGames_xsolla_library = {

    crazyGames_getXSollaToken: function (senderId, onSuccessPtr, onErrorPtr) {
        console.log('library', 'crazyGames_getXSollaToken');
        const onSuccess = (tokenUTF8) => {
            Module.invokeMonoPCallback(senderId, onSuccessPtr, tokenUTF8);
        };
        const onError = () => {
            Module.invokeMonoPCallback(senderId, onErrorPtr);
        };
        try {
            async function getXSollaToken() {
                await Module.waitForPrimeSDK();
                try {
                    const token = await window.CrazyGames.SDK.user.getXsollaUserToken();
                    console.log('library', 'Get Xsolla token result', token);
                    const tokenUTF8 = Module.allocateString(token);
                    onSuccess(tokenUTF8);
                } catch (error) {
                    console.error('library', 'Error getting Xsolla token', error);
                    onError();
                }
            }
            getXSollaToken();
        }
        catch (error) {
            console.error('library', error);
            onError();
        }
    },

    crazyGames_isLoggedIn: function () {
        try {
            if (Module.primeSDK) {
                return Module.primeSDK.player.isLoggedIn;
            }
            else {
                console.log('library', 'primeSDK is not ready');
                return false;
            }
        }
        catch (error) {
            console.error('crazyGames_xsolla_library', error);
            return false;
        }
    },

    crazyGames_invokeLogin: function (senderId, onSuccessPtr, onErrorPtr) {
        console.log('library', 'crazyGames_invokeLogin');
        const onSuccess = () => {
            Module.invokeMonoPCallback(senderId, onSuccessPtr);
        };
        const onError = () => {
            Module.invokeMonoPCallback(senderId, onErrorPtr);
        };
        try {
            if (Module.primeSDK) {
                Module.primeSDK.player.invokeLogin(onSuccess, onError);
            }
            else {
                console.log('library', 'primeSDK is not ready');
                onError();
            }
        }
        catch (error) {
            console.error('library', error);
            onError();
        }
    },

};
mergeInto(LibraryManager.library, crazyGames_xsolla_library);