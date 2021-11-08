using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


public class SpawnObject : MonoBehaviour
{
    [SerializeField]
    private GameObject curObject;

    [SerializeField]
    private string CharacterAddress = string.Empty;



    private void Start()
    {
        curObject = null;
    }

    public void SelectObjectBtn(string _objName)
    {
        CharacterAddress = _objName;

    }

    public void SpawnBtn()
    {
        if(!ReferenceEquals(curObject,null))
        {
            ReleaseObj();
        }

        Addressables.InstantiateAsync("Test_" + CharacterAddress, transform.position, Quaternion.identity).Completed +=
            (AsyncOperationHandle<GameObject> obj) =>
            {
                curObject = obj.Result;
                DestroyObj(((AsyncOperationHandle<GameObject>)obj).Result);
            };

        
    }

    private void DestroyObj(GameObject _obj)
    {
        Destroy(_obj, 5f);
    }

    private void ReleaseObj()
    {
        Addressables.ReleaseInstance(curObject);
    }
}
