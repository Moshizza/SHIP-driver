using Prysm.AppVision.Common;

namespace Prysm.AppVision.AppWizardSalto.ViewModels;

internal class Entity : ObservableObject
{
	private bool _alreadyExist;

	private bool _isEnabled;

	private bool _isSelected;

	private string _name;

	private string _description;

	private string _extId;

	private SaltoDBType _type = SaltoDBType.OfflineDoor;

	private string _extParentId;

	private bool _isOnline;

	private bool _isRoom;

	public bool AlreadyExist
	{
		get
		{
			return _alreadyExist;
		}
		set
		{
			_alreadyExist = value;
			((ObservableObject)this).Notify("AlreadyExist");
		}
	}

	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			_isEnabled = value;
			if (!value)
			{
				IsSelected = false;
			}
			((ObservableObject)this).Notify("IsEnabled");
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = IsEnabled && value;
			((ObservableObject)this).Notify("IsSelected");
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			((ObservableObject)this).Notify("Name");
		}
	}

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			_description = value;
			((ObservableObject)this).Notify("Description");
		}
	}

	public string ExtId
	{
		get
		{
			return _extId;
		}
		set
		{
			_extId = value;
			((ObservableObject)this).Notify("ExtId");
		}
	}

	public string ExtParentId
	{
		get
		{
			return _extParentId;
		}
		set
		{
			_extParentId = value;
			((ObservableObject)this).Notify("ExtParentId");
		}
	}

	public SaltoDBType Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			((ObservableObject)this).Notify("Type");
		}
	}

	public bool IsOnline
	{
		get
		{
			return _isOnline;
		}
		set
		{
			_isOnline = value;
			((ObservableObject)this).Notify("IsOnline");
		}
	}

	public bool IsRoom
	{
		get
		{
			return _isRoom;
		}
		set
		{
			_isRoom = value;
			((ObservableObject)this).Notify("IsRoom");
		}
	}

	public SaltoDBType SaltoType => Type;

	public string TypeDesc => $"{_type}";

	public Entity()
	{
		_alreadyExist = false;
		_isEnabled = true;
		_isSelected = false;
	}

	public string GetVarDesc()
	{
		return Name + (string.IsNullOrWhiteSpace(Description) ? null : (" - " + Description));
	}
}
