using System.Collections.Generic;
using UnityEngine;

public class Node<V> {
	public float key;
	public V val;
	public Node<V> left;
	public Node<V> right;

	public Node(V _val)
	{
		val = _val;
	}

	public Node(V _val, Node<V> _left, Node<V> _right)
	{
		val = _val;
		left = _left;
		right = _right;
	}

	public void SetLeft(Node<V> _left)
	{
		left = _left;
	}

	public void SetRight(Node<V> _right)
	{
		right = _right;
	}
}

public class BSTTest : MonoBehaviour {
	public float[] items;
	Node<int> tree;

	Node<float> BuildTree(float[] _items)
	{
		if (_items.Length < 1)
			return null;
		if (_items.Length == 1)
			return new Node<float> (_items [0]);
		if (_items.Length == 2) 
		{
			float max = Mathf.Max (_items [0], _items [1]);
			float min = Mathf.Min (_items [0], _items [1]);
			Node<float> _result = new Node<float> (max);
			_result.SetLeft (new Node<float> (min));
			return _result;
		}
		int pivotIndex = Random.Range (0, _items.Length - 1);
		Node<float> result = new Node<float> (_items[pivotIndex]);
		List<float> left = new List<float> ();
		List<float> right = new List<float> ();
		for (int i = 0; i < _items.Length; i++) 
		{
			if (i == pivotIndex)
				continue;
			if (_items [i] >= result.val)
				right.Add (_items [i]);
			else
				left.Add (_items [i]);
		}
		result.SetLeft (BuildTree (left.ToArray ()));
		result.SetRight (BuildTree (right.ToArray ()));
		return result;
	}

	void Awake()
	{
		float[] result = new float[items.Length];

	}
}
