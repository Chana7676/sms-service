import { StrictMode, useState } from 'react';
import { createRoot } from 'react-dom/client';
import EmployeesPage from './EmployeesPage';
import MessagesPage from './MessagesPage';
import './style.css';

function App() {
  const [page, setPage] = useState<'employees' | 'messages'>('employees');

  return (
    <main>
      <h1>שליחת SMS לעובדים</h1>
      <nav>
        <button className={page === 'employees' ? 'active' : ''} onClick={() => setPage('employees')}>עובדים</button>
        <button className={page === 'messages' ? 'active' : ''} onClick={() => setPage('messages')}>הודעות</button>
      </nav>
      {page === 'employees' ? <EmployeesPage /> : <MessagesPage />}
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
