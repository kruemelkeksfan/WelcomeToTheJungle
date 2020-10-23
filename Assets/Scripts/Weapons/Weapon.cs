using UnityEngine;
using UnityEngine.UI;

public class Weapon : MonoBehaviour
{
	public virtual bool Safety
	{
		get; set;
	}

	public virtual void PullTrigger()
	{
	}

	public virtual void ReleaseTrigger()
	{
	}

	public virtual void Reload()
	{
	}

	public virtual void SwitchFireMode(int firemode = -1)
	{
	}

	public virtual void UpdateMagazineReadout(Text magazineIndicator)
	{
	}

	public virtual void UpdateFiremodeReadout(Text firemodeIndicator)
	{
	}
}
