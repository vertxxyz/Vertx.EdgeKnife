#nullable enable

#if HAS_GRAPH_TOOLKIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Vertx.EdgeKnife.Editor
{
	/// <inheritdoc />
	/// Used with <see cref="GraphView"/>.
	internal sealed class WireKnifeManipulator : KnifeManipulatorBase
	{
		private readonly GraphView _graphView;

		internal WireKnifeManipulator(GraphView graphView)
		{
			_graphView = graphView;
		}

		protected override bool SupportsAdditive => true;

		protected override void Run(Mode mode, EdgeKnifeElement element)
		{
			switch (mode)
			{
				case Mode.Additive:
					_graphView.Dispatch(
						new ConvertWiresToPortalsCommand(
							GetIntersectingWires()
								.Select(args =>
									{
										Wire wire = args.wire;
										WireModel wireModel = wire.WireModel;
										var outputPort = wireModel.FromPort.GetView<Port>(_graphView);
										var inputPort = wireModel.ToPort.GetView<Port>(_graphView);
										var outputNode = wireModel.FromPort.NodeModel.GetView<NodeView>(_graphView);
										var inputNode = wireModel.ToPort.NodeModel.GetView<NodeView>(_graphView);

										if (outputNode == null || inputNode == null || outputPort == null || inputPort == null)
											return (null, Vector2.zero, Vector2.zero);

										return (wireModel,
											from: outputPort.ChangeCoordinatesTo(wire.contentContainer, outputPort.layout.center),
											to: inputPort.ChangeCoordinatesTo(wire.contentContainer, inputPort.layout.center));
									}
								)
								.Where(args => args.wireModel != null)
								.ToArray(),
							_graphView
						)
					);
					
					// Immediately rename the newly-created ports.
					// Delay until the views have been created.
					_graphView.schedule.Execute(() =>
						{
							using KeyDownEvent evt = KeyDownEvent.GetPooled(char.MinValue, KeyCode.F2, EventModifiers.None);
							evt.target = _graphView.GetSelection().FirstOrDefault()?.GetView(_graphView);
							_graphView.SendEvent(evt);
						}
					).ExecuteLater(10);

					break;
				case Mode.Subtractive:
					_graphView.Dispatch(
						new DeleteElementsCommand(
							GetIntersectingWires()
								.Select(wire => wire.wire.GraphElementModel)
								.ToArray()
						)
					);
					break;
				case Mode.Inactive:
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}

			return;

			IEnumerable<(Wire wire, Vector2 intersection)> GetIntersectingWires()
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
				VisualElement relativeContainer = _graphView.ContentViewContainer;
				Rect pointsBoundsInWireSpace = element.ChangeCoordinatesTo(relativeContainer, pointsBounds);

				FieldInfo controlPointsField = typeof(WireControl)
					.GetField("m_ControlPoints", BindingFlags.NonPublic | BindingFlags.Instance)!;

				// Gather wires within complete bounds.
				using var dispose = ListPool<GraphElement>.Get(out List<GraphElement> elements);
				_graphView.GetGraphElementsInRegion(pointsBoundsInWireSpace, elements, GraphView.PartitioningMode.PlacematTitle);
				foreach (Wire wire in elements.OfType<Wire>())
				{
					// Check the individual control points segments.
					// Accurate render points can't be provided by the GraphToolkit WireControl,
					// will see if it's actually a problem in usage.
					var controlPoints = (Vector2[])controlPointsField.GetValue(wire.WireControl);
					if (controlPoints.Length < 2)
						continue;

					int earliestSegment = points.Count - 1;
					(Wire wire, Vector2 intersection)? result = null;

					Vector2 a = wire.WireControl.parent.ChangeCoordinatesTo(element, controlPoints[0]);
					for (var i = 0; i < controlPoints.Length - 1; i++)
					{
						Vector2 b = wire.WireControl.parent.ChangeCoordinatesTo(element, controlPoints[i + 1]);
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

							result = (wire, intersection);
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
#endif