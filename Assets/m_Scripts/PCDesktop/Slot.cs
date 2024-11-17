using UnityEngine;

interface IExchangable
{
    void ResetPosition(Vector3 position);
}
public class Slot : MonoBehaviour
{
    [SerializeField]
    private BaseItem childInSlot;

    private bool _isSoltSolid;

    public BaseItem ChildInSlot
    {
        get =>childInSlot;
        set
        {
            if (value != null)
            {
                childInSlot.transform.position=transform.position;
                childInSlot.transform.parent=transform;
                childInSlot = value;
            }
            else
            {
                childInSlot.transform.parent=null;
                childInSlot = value;
            }
        }
    }

    public bool IsCloseToThisSlot(GameObject target,float distance)
    {
        return Vector3.Distance(transform.position, target.transform.position) < distance;
    }

    public bool PlaceGameObjectInSlot(BaseItem target, Vector3 position)
    {
        if(_isSoltSolid) return false;
        else if (childInSlot != null)
        {
            return ExchangeGameObjectInSlot(target, position);
        }
        else
        {
            return FillSlot(target, position);
        }
    }

    private bool FillSlot(BaseItem target, Vector3 position)
    {
        ChildInSlot = target;
        return true;
    }
    private bool ExchangeGameObjectInSlot(BaseItem target,Vector3 resetPosition)
    {
        childInSlot.GetComponent<IExchangable>().ResetPosition(resetPosition);
        ChildInSlot = null;
        ChildInSlot=target;
        return true;
    }
}
