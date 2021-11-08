using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ChangeImage : MonoBehaviour
{
    [SerializeField]
    private Image curImg;

    [SerializeField]
    private string CharacterAddress;



    public void ChangeImgBtn()
    {
        Addressables.LoadAssetAsync<Sprite>(CharacterAddress).Completed +=
            (AsyncOperationHandle<Sprite> sprite) =>
            {
                Debug.Log(sprite.DebugName);
                curImg.sprite = sprite.Result;
            };

    }

}
