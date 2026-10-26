#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.EdgeKnife.Editor
{
	/// <inheritdoc />
	/// Used with <see cref="GraphView"/>.
	public sealed class EdgeKnifeManipulator : KnifeManipulatorBase
	{
		private readonly GraphView _graphView;
		private readonly Action<Vector2, IEnumerable<Edge>>? _createRedirect;

		protected override bool SupportsAdditive => _createRedirect != null;

		public EdgeKnifeManipulator(GraphView graphView, Action<Vector2, IEnumerable<Edge>> createRedirect)
		{
			_graphView = graphView;
			_createRedirect = createRedirect;
		}

		protected override void Run(Mode mode, EdgeKnifeElement element)
		{
			IEnumerable<(Edge edge, Vector2 intersection)> edgeIntersections = GetIntersectingEdges();
			switch (mode)
			{
				case Mode.Additive:
					if (_createRedirect == null)
						break;

					// Add redirect nodes.

					// Collect edges by their destination port
					Dictionary<Port, List<(Edge, Vector2)>> edgesByDestination = new();
					foreach ((Edge edge, Vector2 intersection) edgeIntersection in edgeIntersections)
					{
						if (!edgesByDestination.TryGetValue(edgeIntersection.edge.output, out List<(Edge, Vector2)> list))
						{
							edgesByDestination.Add(edgeIntersection.edge.output, list = new List<(Edge, Vector2)>());
						}

						list.Add(edgeIntersection);
					}

					foreach ((_, List<(Edge edge, Vector2 position)> edges) in edgesByDestination)
					{
						Vector2 position = edges.Aggregate(Vector2.zero, (a, b) => a + b.position) / edges.Count;
						position = element.ChangeCoordinatesTo(_graphView.contentViewContainer, position);
						_createRedirect.Invoke(position, edges.Select(e => e.edge));
					}

					break;
				case Mode.Subtractive:
					HashSet<Edge> toRemove = edgeIntersections.Select(e => e.edge).ToHashSet();
					_graphView.DeleteElements(toRemove);
					break;
				case Mode.Inactive:
				default:
					throw new ArgumentOutOfRangeException();
			}

			return;

			IEnumerable<(Edge edge, Vector2 intersection)> GetIntersectingEdges()
			{
				IReadOnlyList<Vector2> points = element.Points;
				if (points.Count < 2)
				{
					yield break;
				}

				Vector2 min = points[0];
				Vector2 max = min;

				for (var i = 1; i < points.Count; i++)
				{
					Vector2 point = points[i];
					min.x = Math.Min(min.x, point.x);
					min.y = Math.Min(min.y, point.y);
					max.x = Math.Max(max.x, point.x);
					max.y = Math.Max(max.y, point.y);
				}

				Rect pointsBounds = new(min, max - min);

				VisualElement relativeContainer = _graphView.contentViewContainer;
				Rect pointsBoundsInEdgeSpace = element.ChangeCoordinatesTo(relativeContainer, pointsBounds);

				FieldInfo renderPointsField = typeof(EdgeControl)
					.GetField("m_RenderPoints", BindingFlags.NonPublic | BindingFlags.Instance)!;

				// Check edge intersections.
				foreach (Edge edge in _graphView.edges)
				{
					// Conservative check for the bounds of the points array.
					if (!edge.Overlaps(pointsBoundsInEdgeSpace))
					{
						continue;
					}

					// Check the individual render points segments.
					EdgeControl edgeControl = edge.edgeControl;
					var renderPoints = (List<Vector2>)renderPointsField.GetValue(edgeControl);
					if (renderPoints.Count < 2)
						continue;

					int earliestSegment = points.Count - 1;
					(Edge edge, Vector2 intersection)? result = null;

					Vector2 a = edgeControl.ChangeCoordinatesTo(element, renderPoints[0]);
					for (var i = 0; i < renderPoints.Count - 1; i++)
					{
						Vector2 b = edgeControl.ChangeCoordinatesTo(element, renderPoints[i + 1]);
						// Conservative check per-segment.
						if (!RectUtils.IntersectsSegment(pointsBounds, a, b))
						{
							a = b;
							continue;
						}

						// Thorough per-segment check.
						for (var j = 0; j < earliestSegment; j++)
						{
							if (!EdgeKnifeElement.TryGetSegmentIntersection(
									a,
									b,
									points[j],
									points[j + 1],
									out Vector2 intersection
								)
							)
								continue;

							result = (edge, intersection);
							earliestSegment = j;
							break;
						}

						a = b;
					}

					if (result.HasValue)
						yield return result.Value;
				}
			}
		}
	}
}