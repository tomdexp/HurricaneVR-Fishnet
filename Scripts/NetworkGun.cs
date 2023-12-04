using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

//Client Authoritative gun solution
//For Server Authoritative the server would
//need to manage gun cocking and ammo count
//and the client would just need to pass its trigger state
public class NetworkGun : NetworkBehaviour
{
    private CustomHVRGunBase _hvrGunBase;

    private void Awake()
    {
        _hvrGunBase = GetComponent<CustomHVRGunBase>();
        if (_hvrGunBase == null)
        {
            Debug.LogError("NetworkGun requires a CustomHVRGunBase component, " +
                           "click on the message to select the GameObject with the issue", this);
            return;
        }
        _hvrGunBase.Fired.AddListener(OnFired);
    }
    private void OnDestroy()
    {
        if (_hvrGunBase) _hvrGunBase.Fired.RemoveListener(OnFired);
    }
    private void OnFired()
    {
        if (Owner.IsLocalClient)
        {
            RPCShoot();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        //The server can always shoot upon request of a client
        _hvrGunBase.RequiresAmmo = false;
        _hvrGunBase.RequiresChamberedBullet = false;
    }

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        //Only the owner need to track shooting requirements
        if (Owner.IsLocalClient)
        {
            _hvrGunBase.RequiresAmmo = true;
            _hvrGunBase.RequiresChamberedBullet = true;
        }
        else
        {
            //The observers always shoot upon request of the server
            _hvrGunBase.RequiresAmmo = false;
            _hvrGunBase.RequiresChamberedBullet = false;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void RPCShoot()
    {
        _hvrGunBase.NetworkShoot();
        ObserversShoot();
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ObserversShoot()
    {
        _hvrGunBase.NetworkShoot();
    }
}
