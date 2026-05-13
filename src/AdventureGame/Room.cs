namespace AdventureGame;

public class Room
{
	private bool isLit;
	private bool hasLamp;
	private bool hasKey;
	private bool hasChest;

	private bool hasNorth;
	private bool hasSouth;
	private bool hasEast;
	private bool hasWest;

	private string description;

	public Room()
	{
		SetLit(false);
		SetLamp(false);
		SetKey(false);
		SetChest(false);
		SetNorth(false);
		SetSouth(false);
		SetEast(false);
		SetWest(false);
		SetDescription(string.Empty);
	}

	public bool IsLit()
	{
		return isLit;
	}

	public bool HasLamp()
	{
		return hasLamp;
	}

	public bool HasKey()
	{
		return hasKey;
	}

	public bool HasChest()
	{
		return hasChest;
	}

	public bool HasNorth()
	{
		return hasNorth;
	}

	public bool HasSouth()
	{
		return hasSouth;
	}

	public bool HasEast()
	{
		return hasEast;
	}

	public bool HasWest()
	{
		return hasWest;
	}

	public string GetDescription()
	{
		return description;
	}

	public void SetLit(bool b)
	{
		isLit = b;
	}

	public void SetLamp(bool b)
	{
		hasLamp = b;
	}

	public void SetKey(bool b)
	{
		hasKey = b;
	}

	public void SetChest(bool b)
	{
		hasChest = b;
	}

	
	public void SetNorth(bool b)
	{
		hasNorth = b;
	}

	public void SetSouth(bool b)
	{
		hasSouth = b;
	}

	public void SetEast(bool b)
	{
		hasEast = b;
	}

	public void SetWest(bool b)
	{
		hasWest = b;
	}

	public void SetDescription(string d)
	{
		description = d;
	}

	public override string ToString()
	{
		return $"Room[isLit={isLit}, hasLamp={hasLamp}, hasKey={hasKey}, hasChest={hasChest}, hasNorth={hasNorth}, hasSouth={hasSouth}, hasEast={hasEast}, hasWest={hasWest}, description={description}]";
	}
}
