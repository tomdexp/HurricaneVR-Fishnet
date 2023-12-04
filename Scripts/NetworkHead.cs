using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkHead : NetworkBehaviour
{
    [FormerlySerializedAs("headToScale")] [SerializeField] private Transform _headToScale;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!Owner.IsLocalClient)
        {
            //Show the head on remote players for FinalIK
            if (_headToScale)
            {
                _headToScale.localScale = Vector3.one;
            }
        }
    }
}
