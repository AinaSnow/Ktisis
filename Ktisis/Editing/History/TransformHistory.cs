using System.Linq;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Common.Math;

using Ktisis.History;
using Ktisis.Scene.Objects;
using Ktisis.Editing.History.Actions;

namespace Ktisis.Editing.History; 

public class TransformHistory : HistoryClient<TransformAction> {
	public TransformHistory(string id, HistoryService _history) : base(id, _history) {}

	protected override TransformAction Create() => new(HandlerId);

	public bool UpdateOrBegin(SceneObject target, Matrix4x4 targetMx) {
		var targetId = target.UiId;
		var begin = this.Action == null || this.Action.TargetId != targetId;
		if (begin) {
			Begin();
			this.Action!.TargetId = targetId;
			this.Action.Initial = targetMx;
		}

		this.Action!.Final = targetMx;

		return begin;
	}

	public void AddSubjects(IEnumerable<SceneObject> subjects) {
		if (this.Action == null) return;
		this.Action.SubjectIds.Clear();
		this.Action.SubjectIds.AddRange(subjects.Select(item => item.UiId));
	}
}
