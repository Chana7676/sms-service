import { useEffect, useState } from 'react';
import { Employee, getEmployees, sendMessage } from './api';

const key = (e: Employee) => `${e.firstName}|${e.lastName}|${e.address}`;
const MAX_LENGTH = 335;

export default function EmployeesPage() {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [search, setSearch] = useState('');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [body, setBody] = useState('');
  const [sending, setSending] = useState(false);
  const [result, setResult] = useState('');

  useEffect(() => { getEmployees().then(setEmployees); }, []);

  const shown = employees.filter(e => `${e.firstName} ${e.lastName} ${e.address}`.includes(search));
  const selectable = shown.filter(e => e.hasValidPhone);
  const allShownSelected = selectable.length > 0 && selectable.every(e => selected.has(key(e)));

  function toggle(e: Employee) {
    const next = new Set(selected);
    next.has(key(e)) ? next.delete(key(e)) : next.add(key(e));
    setSelected(next);
  }

  // "Select all" works on what the search shows, and keeps selections made under other searches.
  function toggleAllShown() {
    const next = new Set(selected);
    selectable.forEach(e => (allShownSelected ? next.delete(key(e)) : next.add(key(e))));
    setSelected(next);
  }

  async function send() {
    const recipients = employees.filter(e => selected.has(key(e)));
    if (recipients.length > 20 && !confirm(`לשלוח ל-${recipients.length} עובדים?`)) return;

    setSending(true);
    const failures: string[] = [];
    for (const employee of recipients) {
      const error = await sendMessage(employee, body);
      if (error) failures.push(`${employee.firstName} ${employee.lastName}: ${error}`);
    }
    setSending(false);
    setResult(`התקבלו ${recipients.length - failures.length} הודעות לשליחה.` +
      (failures.length ? ` נכשלו ${failures.length}: ${failures.join(' · ')}` : ''));
    setSelected(new Set());
    setBody('');
  }

  return (
    <section>
      <div className="toolbar">
        <input placeholder="חיפוש לפי שם או כתובת" value={search} onChange={e => setSearch(e.target.value)} />
        <label>
          <input type="checkbox" checked={allShownSelected} onChange={toggleAllShown} /> בחר את כל העובדים
        </label>
      </div>

      <table>
        <thead>
          <tr>
            <th></th>
            <th>שם</th><th>כתובת</th><th>טלפון</th>
          </tr>
        </thead>
        <tbody>
          {shown.map(e => (
            <tr key={key(e)} className={e.hasValidPhone ? '' : 'invalid'}>
              <td><input type="checkbox" disabled={!e.hasValidPhone} checked={selected.has(key(e))} onChange={() => toggle(e)} /></td>
              <td>{e.firstName} {e.lastName}</td>
              <td>{e.address}</td>
              <td>{e.phone}{!e.hasValidPhone && ' ⚠ טלפון לא תקין'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="compose">
        <textarea placeholder="תוכן ההודעה" maxLength={MAX_LENGTH} value={body} onChange={e => setBody(e.target.value)} />
        <div>{body.length} / {MAX_LENGTH}</div>
        <button disabled={sending || selected.size === 0 || !body.trim()} onClick={send}>
          {sending ? 'שולח...' : `שלח ל-${selected.size} עובדים`}
        </button>
        {result && <p>{result}</p>}
      </div>
    </section>
  );
}
