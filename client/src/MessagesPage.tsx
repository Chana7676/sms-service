import { useEffect, useState } from 'react';
import { Message, getMessages } from './api';

const STATUSES = { Pending: 'ממתינה', Sent: 'נשלחה', Failed: 'נכשלה' };

export default function MessagesPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [status, setStatus] = useState('');

  // Load now and every 3 seconds, so a Pending message is seen turning into Sent / Failed.
  useEffect(() => {
    const load = () => getMessages(status).then(setMessages);
    load();
    const timer = setInterval(load, 3000);
    return () => clearInterval(timer);
  }, [status]);

  return (
    <section>
      <select value={status} onChange={e => setStatus(e.target.value)}>
        <option value="">כל הסטטוסים</option>
        {Object.entries(STATUSES).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
      </select>

      <table>
        <thead>
          <tr><th>#</th><th>זמן</th><th>עובד</th><th>טלפון</th><th>הודעה</th><th>סטטוס</th><th>ניסיונות</th><th>שגיאה</th></tr>
        </thead>
        <tbody>
          {messages.map(m => (
            <tr key={m.id}>
              <td>{m.id}</td>
              <td>{new Date(m.createdAt).toLocaleString('he-IL')}</td>
              <td>{m.to.firstName} {m.to.lastName}</td>
              <td>{m.to.phone}</td>
              <td>{m.body}</td>
              <td className={m.status}>{STATUSES[m.status]}</td>
              <td>{m.attempts}</td>
              <td>{m.error}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {messages.length === 0 && <p>אין הודעות.</p>}
    </section>
  );
}
