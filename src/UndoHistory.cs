using System;using System.Collections.Generic;
namespace BeamScheduleFormatter {
// History moves only after a successful apply, and a new action invalidates redo.
sealed class UndoHistory<T> {
 readonly List<T> undo=new List<T>(),redo=new List<T>();readonly int capacity;
 internal UndoHistory(int capacity=40){if(capacity<1)throw new ArgumentOutOfRangeException("capacity");this.capacity=capacity;}
 internal bool CanUndo{get{return undo.Count>0;}}internal bool CanRedo{get{return redo.Count>0;}}
 internal T NextUndo{get{return undo[undo.Count-1];}}internal T NextRedo{get{return redo[redo.Count-1];}}
 internal void Record(T action){undo.Add(action);if(undo.Count>capacity)undo.RemoveAt(0);redo.Clear();}
 internal void Undo(Action<T> apply){if(!CanUndo)return;T item=NextUndo;apply(item);undo.RemoveAt(undo.Count-1);redo.Add(item);}
 internal void Redo(Action<T> apply){if(!CanRedo)return;T item=NextRedo;apply(item);redo.RemoveAt(redo.Count-1);undo.Add(item);}
 internal void LimitCost(Func<T,long> cost,long budget){long total=0;foreach(var item in undo)total+=cost(item);while(undo.Count>1&&total>budget){total-=cost(undo[0]);undo.RemoveAt(0);}}
 internal void Clear(){undo.Clear();redo.Clear();}
}
}
