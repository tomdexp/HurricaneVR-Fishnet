using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

public class NetworkGrabbable : NetworkBehaviour
{
    private HVRGrabbable _hvrGrabbable;
    private Rigidbody _rigidbody;
    private bool _isSocketed;
    [SyncVar(WritePermissions = WritePermission.ServerOnly)]
    private int _socketId = -1;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _hvrGrabbable = GetComponent<HVRGrabbable>();
        if (_rigidbody == null)
        {
            Debug.LogError("NetworkGrabbable requires a Rigidbody component, " +
                           "click on the message to select the GameObject with the issue", this);
            return;
        }
        if (_hvrGrabbable == null)
        {
            Debug.LogError("NetworkGrabbable requires a HVRGrabbable component, " +
                           "click on the message to select the GameObject with the issue", this);
            return;
        }
        
        _hvrGrabbable.Grabbed.AddListener(OnGrabbed);
        _hvrGrabbable.Socketed.AddListener(OnSocketed);
        _hvrGrabbable.UnSocketed.AddListener(OnUnSocketed);
    }

    private void OnDestroy()
    {
        _hvrGrabbable.Grabbed.RemoveListener(OnGrabbed);
        _hvrGrabbable.Socketed.RemoveListener(OnSocketed);
        _hvrGrabbable.UnSocketed.RemoveListener(OnUnSocketed);
    }

    //------------------------------------- HVR Event Listeners -----------------------------------------
    private void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
    {
        if (grabber.IsSocket) return;
        RPCSendTakeover();
        if (_rigidbody)
        {
            _rigidbody.isKinematic = false;
        }
        //Debug.Log("Grabbed", gameObject);
    }

    private void OnSocketed(HVRSocket socket, HVRGrabbable grabbable)
    {
        if (Owner.IsLocalClient)
        {
            var id = 0;
            if (socket.TryGetComponent<NetworkObject>(out var networkObject))
            {
                id = networkObject.ObjectId;
            }
            else
            {
                Debug.LogWarning("Socket does not have a network object");
            }

            RPCSocket(id);
            //Debug.Log("Socketed", gameObject);
        }
    }
    private void OnUnSocketed(HVRSocket socket, HVRGrabbable grabbable)
    {
        RPCUnSocket();
        //Debug.Log("UnSocketed", gameObject);      
    }

    //------------------------------------- Server Functions -----------------------------------------
    public override void OnStartServer()
    {
        ServerManager.Objects.OnPreDestroyClientObjects += OnPreDestroyClientObjects;
        if (_hvrGrabbable.StartingSocket != null) {
            var id = 0;
            if (_hvrGrabbable.StartingSocket.TryGetComponent<NetworkObject>(out var networkObject))
            {
                _socketId = networkObject.ObjectId;
                TrySocket(_socketId);
                //If the starting socket is not linked remove it or it can cause issues
                if (!_hvrGrabbable.LinkStartingSocket)
                {
                    _hvrGrabbable.StartingSocket = null;
                }
            }
            else
            {
                Debug.LogWarning("Socket does not have a network object");
            }
        }
    }
    public override void OnStopServer()
    {
        ServerManager.Objects.OnPreDestroyClientObjects -= OnPreDestroyClientObjects;
    }

    //Preserve grabbable network objects when the owner client disconnects
    private void OnPreDestroyClientObjects(NetworkConnection conn)
    {
        if (conn == Owner)
            RemoveOwnership();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RPCSendTakeover(NetworkConnection conn = null)
    {
        //They are already the owner do nothing
        if (Owner.ClientId == conn.ClientId) return;
        if(_socketId > -1)
        {
            _socketId = -1;
            TryUnSocket();
            ObserversUnSocketedGrabbable();
        }
        NetworkObject.RemoveOwnership();
        NetworkObject.GiveOwnership(conn);
        //Debug.Log("Server Grants Ownership to " + conn.ClientId, gameObject);  
    }

    [ServerRpc(RequireOwnership = true)]
    public void RPCSocket(int _socketId)
    {
        this._socketId = _socketId;
        TrySocket(this._socketId);
        ObserversSocketedGrabbable(this._socketId);
    }
    [ServerRpc(RequireOwnership = true)]
    public void RPCUnSocket()
    {
        _socketId = -1;
        TryUnSocket();
        ObserversUnSocketedGrabbable();
        //Socketing can remove rigidbodies, we need to try and get it again
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
        //The server is the owner
        if (!Owner.IsValid && _rigidbody != null)
        {
            _rigidbody.isKinematic = false;
        }
    }
    public override void OnOwnershipServer(NetworkConnection prevOwner)
    {
        base.OnOwnershipServer(prevOwner);
        if (_rigidbody == null) return;
        //The server has become the owner
        if (!Owner.IsValid)
        {
            if (_socketId > - 1)
            {
                _rigidbody.isKinematic = true;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                _rigidbody.isKinematic = false;
            }
        }
        else
        {
            _rigidbody.isKinematic = true;
        }
    }

    //------------------------------------- Client Functions -----------------------------------------
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (_socketId > -1)
        {
            TrySocket(_socketId, true);
        }
        else
        {
            TryUnSocket();
            if (_hvrGrabbable.StartingSocket != null && !_hvrGrabbable.LinkStartingSocket)
            {
                //Remove starting sockets that are no longer valid
                _hvrGrabbable.StartingSocket = null;
            }
        }
    }
    [ObserversRpc(ExcludeOwner = true)]
    public void ObserversSocketedGrabbable(int _socketId)
    {
        TrySocket(_socketId);
    }

    private void TrySocket(int _socketId, bool ignoreGrabSound = false)
    {
        //Find the network object socket if this isn't socketed
        if (!_hvrGrabbable.IsSocketed)
        {
            var netObjects = FindObjectsOfType<NetworkObject>(true);
            foreach (var netObj in netObjects)
            {
                if (netObj.ObjectId == _socketId)
                {
                    if (netObj.TryGetComponent<HVRSocket>(out var socket))
                    {
                        if (_rigidbody != null)
                        {
                            if (Owner.IsLocalClient)
                            {
                                _rigidbody.isKinematic = false;
                            }
                            else
                            {
                                _rigidbody.isKinematic = true;
                            }
                        }
                        socket.TryGrab(_hvrGrabbable, true, ignoreGrabSound);
                        if(_rigidbody != null) _rigidbody.isKinematic = true;
                        //Debug.Log("Socketed on client", gameObject);
                        break;
                    }
                }
            }
        }
        //Parent this to the socket
        _isSocketed = true;
    }

    [ObserversRpc(ExcludeOwner = true)]
    public void ObserversUnSocketedGrabbable()
    {
        TryUnSocket();
        _isSocketed = false;
    }

    private void TryUnSocket()
    {
        //Socketing can remove rigidbodies, we need to try and get it again
        if(_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
        if (_hvrGrabbable.IsSocketed)
        {
            //Debug.Log("Unsocketed on client");
            _hvrGrabbable.Socket.ForceRelease();
        }
    }

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        if (_rigidbody)
        {
            if (Owner.IsLocalClient && !_isSocketed)
            {
                _rigidbody.isKinematic = false;
            }
            else
            {
                _rigidbody.isKinematic = true;
            }
        }
        //Debug.Log("Client Ownership sets kinematic " + rb.isKinematic, gameObject);
    }
}
