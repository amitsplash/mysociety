/** Local calendar date as yyyy-MM-dd */
export function todayIsoDate(): string {
  return formatLocalIsoDate(new Date());
}

export function formatLocalIsoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function parseIsoDate(value: string): Date | null {
  const trimmed = value.trim();
  if (!/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
    return null;
  }
  const [y, m, d] = trimmed.split('-').map(Number);
  const date = new Date(y, m - 1, d);
  if (date.getFullYear() !== y || date.getMonth() !== m - 1 || date.getDate() !== d) {
    return null;
  }
  return date;
}

export function isFutureIsoDate(value: string): boolean {
  const parsed = parseIsoDate(value);
  if (!parsed) {
    return true;
  }
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  parsed.setHours(0, 0, 0, 0);
  return parsed.getTime() > today.getTime();
}

/** ISO 8601 date at UTC midnight for API */
export function toApiExpenseDate(isoDate: string): string {
  return `${isoDate.trim()}T00:00:00.000Z`;
}

export function validateExpenseDateInput(value: string): string | null {
  if (!parseIsoDate(value)) {
    return 'Enter a valid date (YYYY-MM-DD)';
  }
  if (isFutureIsoDate(value)) {
    return 'Expense date cannot be in the future';
  }
  return null;
}
