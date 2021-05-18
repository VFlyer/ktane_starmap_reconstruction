using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class Starmap {
	private int _size;
	private bool[][] _data;

	public Starmap(int size) {
		_size = size;
		_data = new bool[size][];
		for (int i = 0; i < size; i++) _data[i] = new bool[size];
	}

	public bool Add(int from, int to) {
		if (_data[from][to]) return false;
		_data[from][to] = true;
		_data[to][from] = true;
		return true;
	}

	public override string ToString() {
		List<string> connections = new List<string>();
		for (int i = 0; i < _size; i++) for (int j = i + 1; j < _size; j++) if (_data[i][j]) connections.Add(string.Format("{0}-{1}", i, j));
		return string.Format("starmap ({0}): {1}", _size, connections.Count == 0 ? "none" : connections.Join(";"));
	}

	public int GetAdjacentNodesCount(int nodeId) {
		return _data[nodeId].Select(b => b ? 1 : 0).Sum();
	}

	public IEnumerable<int> GetAdjacentNodes(int nodeId) {
		return Enumerable.Range(0, _size).Where(i => _data[nodeId][i]);
	}

	public int GetDistance(int from, int to) {
		int[] length = Enumerable.Range(0, _size).Select(_ => -1).ToArray();
		length[from] = 0;
		Queue<int> q = new Queue<int>(new[] { from });
		while (length[to] == -1 && q.Count > 0) {
			int node = q.Dequeue();
			foreach (int otherNode in GetAdjacentNodes(node)) {
				if (length[otherNode] != -1) continue;
				length[otherNode] = length[node] + 1;
				q.Enqueue(otherNode);
			}
		}
		return length[to];
	}

	public string ToShortString() {
		Starmap loggedCorridors = new Starmap(_size);
		List<string> result = new List<string>();
		int i = 0;
		while (i < _size) {
			List<int> path = new List<int>(new[] { i });
			while (true) {
				int from = path.Last();
				for (int to = 0; to < _size; to++) {
					if (_data[from][to] && !loggedCorridors._data[from][to]) {
						path.Add(to);
						loggedCorridors.Add(from, to);
						break;
					}
				}
				if (from == path.Last()) break;
			}
			if (path.Count == 1) i++;
			else result.Add(path.Join("-"));
		}
		return result.Join("; ");
	}
}
