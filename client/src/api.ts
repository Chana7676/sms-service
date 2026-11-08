// Calls to the API. In development Vite forwards /employees and /messages to http://localhost:5000.

export type Employee = { firstName: string; lastName: string; address: string; phone: string; hasValidPhone: boolean };

export type Message = {
  id: number;
  to: Omit<Employee, 'hasValidPhone'>;
  body: string;
  status: 'Pending' | 'Sent' | 'Failed';
  error: string | null;
  attempts: number;
  createdAt: string;
};

export async function getEmployees(): Promise<Employee[]> {
  return (await fetch('/employees')).json();
}

export async function getMessages(status: string): Promise<Message[]> {
  const response = await fetch(`/messages?pageSize=200${status ? `&status=${status}` : ''}`);
  return (await response.json()).items;
}

/** Returns null on success, or the error text from the API. */
export async function sendMessage(employee: Employee, body: string): Promise<string | null> {
  const { hasValidPhone, ...to } = employee;
  const response = await fetch('/messages', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ to, body }),
  });
  if (response.ok) return null;
  const problem = await response.json();
  return problem.detail ?? problem.title;
}
