import type { Task } from './api/grantsApi'

/** What the task table needs: the grant's tasks, most urgent first. */
export interface TaskListProps {
  tasks: Task[]
  onComplete: (taskId: number) => void
}

/** The grant's tasks, with overdue tasks highlighted. */
export default function TaskList({ tasks, onComplete }: TaskListProps) {
  if (tasks.length === 0) {
    return <p>No tasks yet.</p>
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Title</th>
          <th>Priority</th>
          <th>Due</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {tasks.map((task) => (
          <tr key={task.id} className={task.isOverdue ? 'overdue' : undefined}>
            <td>{task.title}</td>
            <td>{task.priority}</td>
            <td>{task.dueDate}</td>
            <td>
              {task.completedDate ? (
                <>
                  <span>{`Completed on ${task.completedDate}`}</span>
                  {task.completedLate && <span className="label">Completed late</span>}
                </>
              ) : (
                <>
                  {/* A text label too, so overdue does not rely on colour alone */}
                  {task.isOverdue && <span className="label">Overdue</span>}
                  <button type="button" onClick={() => onComplete(task.id)}>
                    Complete
                  </button>
                </>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
