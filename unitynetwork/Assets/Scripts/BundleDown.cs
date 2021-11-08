using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class BundleDown : MonoBehaviour
{
    [SerializeField] private Text SizeTxt;


    [SerializeField]
    private string LableForBundleDown = string.Empty;

    public void BundleDownBtn()
    {
        Addressables.DownloadDependenciesAsync(LableForBundleDown).Completed +=
            (AsyncOperationHandle Handle) =>
            {
                Debug.Log("Success Download");

                Addressables.Release(Handle);
            };
    }

    public void CheckFileSizeBtn()
    {
        Addressables.GetDownloadSizeAsync(LableForBundleDown).Completed +=
            (AsyncOperationHandle<long> SizeHandle) =>
            {
                string sizeTxt = string.Concat(SizeHandle.Result, " byte");

                SizeTxt.text = sizeTxt;

                Addressables.Release(SizeHandle);
            };
    }
}
