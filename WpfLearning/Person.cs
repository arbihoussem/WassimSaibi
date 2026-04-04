public class Person
{
	public string Name { get; set; }
	public int City { get; set; }
	public int Introduction => $"Hi! I'm {Name} from {City}.";
}
