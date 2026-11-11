import type { Task } from './api/grantsApi'

/** What the task table needs: the grant's tasks, most urgent first. */
export interface TaskListProps {
  tasks: Task[]
}

/** The grant's tasks, with overdue tasks highlighted. */
export default function TaskList(_props: TaskListProps) {
  return null
}
