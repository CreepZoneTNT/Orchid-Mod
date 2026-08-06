using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace OrchidMod.Utilities;

public struct Circle(Vector2 origin, float radius)
{
	public Vector2 Origin = origin;
	public float Radius = radius;

	public static (Vector2, float) ToTuple(Circle value) => (value.Origin, value.Radius);

	public static float Circumference(Circle value) => 2 * MathHelper.Pi * value.Radius;

	public static float Area(Circle value) => MathHelper.Pi * value.Radius * value.Radius;

	public static bool HasPointInCircle(Circle circle, Vector2 point) => point.DistanceSQ(circle.Origin) <= circle.Radius * circle.Radius;
	
	public static Vector2 ClosestPointInCircle(Circle circle, Vector2 point)
	{
		if (point.DistanceSQ(circle.Origin) <= circle.Radius * circle.Radius)
			return point;
		return circle.Origin.DirectionTo(point) * circle.Radius;
	}
	
	public static Circle CircularizeHitbox(Entity entity, float lenience = 1, bool useEntity1LargestDim = true)
	{
		float radius = entity.GetDimension(useEntity1LargestDim) * 0.5f;
		return new Circle(entity.Center, radius * lenience);
	}

	public static Circle operator +(Circle circle, Vector2 point) => new Circle(circle.Origin + point, circle.Radius);
	public static Circle operator +(Circle circle, float radius) => new Circle(circle.Origin, circle.Radius + radius);
	
	public static Circle operator *(Circle circle, float amount) => new Circle(circle.Origin, circle.Radius * amount);
	public static Circle operator /(Circle circle, float amount) => new Circle(circle.Origin, circle.Radius / amount);

}
	

public static partial class OrchidUtils
{
	
	public static int GetLargestDimension(this Entity entity) => Math.Max(entity.width, entity.height);
	public static int GetSmallestDimension(this Entity entity) => Math.Min(entity.width, entity.height);
	public static int GetDimension(this Entity entity, bool useLargestDim = true) => useLargestDim ? entity.GetLargestDimension() : entity.GetSmallestDimension();

	public static (float, float) XYTuple(this Vector2 vector) => (vector.X, vector.Y);
	
	public static bool HasPointInCircle(this Circle circle, Vector2 point) => Circle.HasPointInCircle(circle, point);	
	public static Vector2 ClosestPointInCircle(this Circle circle, Vector2 point) => Circle.ClosestPointInCircle(circle, point);
	public static Circle CircularizeHitbox(this Entity entity, float lenience = 1, bool useEntity1LargestDim = true) => Circle.CircularizeHitbox(entity, lenience, useEntity1LargestDim);
	
	public static bool CheckCircularvCircularCollision(Vector2 origin1, float radius1, Vector2 origin2, float radius2) => origin1.Distance(origin2) <= radius1 + radius2;
	public static bool CheckCircularvCircularCollision(Circle circle1, Circle circle2) => CheckCircularvCircularCollision(circle1.Origin, circle1.Radius, circle2.Origin, circle2.Radius);
	public static bool CheckCircularvCircularCollision(Entity entity1, Entity entity2, bool useEntity1LargestDim = true, bool useEntity2LargestDim = true) => CheckCircularvCircularCollision(entity1.Center, entity1.GetDimension(useEntity1LargestDim) * 0.5f, entity2.Center, entity2.GetDimension(useEntity2LargestDim) * 0.5f);

	/// <summary> A somewhat scuffed way to tell if a circular hitbox is overlapping a rectangle. </summary>
	public static bool CheckAABBvCircularCollision(Rectangle rectangle, Circle circle)
	{
		Vector2 refPointX = rectangle.Center() + Vector2.UnitX * rectangle.Width / 4f * (circle.Origin.X > rectangle.Center().X).ToDirectionInt();
		Vector2 refPointY = rectangle.Center() + Vector2.UnitY * rectangle.Height / 4f * (circle.Origin.Y > rectangle.Center().Y).ToDirectionInt();
		return rectangle.Contains(Circle.ClosestPointInCircle(circle, rectangle.Center()).ToPoint()) || rectangle.Contains(Circle.ClosestPointInCircle(circle, refPointX).ToPoint()) || rectangle.Contains(Circle.ClosestPointInCircle(circle, refPointY).ToPoint());
	}
	
	public static bool CheckAABBvCircularCollision(Vector2 rectPosition, Vector2 rectDimensions, Vector2 circOrigin, float circRadius)
	{
		Rectangle rectangle = new Rectangle((int)rectPosition.X, (int)rectPosition.Y, (int)rectDimensions.X, (int)rectDimensions.Y);
		Circle circle = new Circle(circOrigin, circRadius);
		return CheckAABBvCircularCollision(rectangle, circle);
	}

	public static Vector2 Scale(this Vector2 value, float factorX = 1f, float factorY = 1f) => new Vector2(value.X * factorX, value.Y * factorY);
	public static void Scale(ref Vector2 original, out Vector2 result, float factorX = 1f, float factorY = 1f) => result = new Vector2(original.X * factorX, original.Y * factorY);

}