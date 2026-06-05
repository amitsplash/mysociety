const MONTH_REGEX = /^(\d{4})-(0[1-9]|1[0-2])$/;

export function getRecentMonths(count = 12, date = new Date()): string[] {
  const months: string[] = [];
  let year = date.getFullYear();
  let month = date.getMonth() + 1;

  for (let i = 0; i < count; i++) {
    months.push(`${year}-${String(month).padStart(2, '0')}`);
    if (month === 1) {
      year -= 1;
      month = 12;
    } else {
      month -= 1;
    }
  }

  return months.reverse();
}

export function getCurrentMonth(date = new Date()): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
}

export function addMonths(yyyyMm: string, monthsToAdd: number): string {
  const parsed = parseMonth(yyyyMm);
  if (!parsed) throw new Error('Invalid month');
  const index = parsed.year * 12 + (parsed.month - 1) + monthsToAdd;
  const year = Math.floor(index / 12);
  const month = (index % 12) + 1;
  return `${year}-${String(month).padStart(2, '0')}`;
}

export function countInclusiveMonths(fromMonth: string, toMonth: string): number {
  const from = parseMonth(fromMonth);
  const to = parseMonth(toMonth);
  if (!from || !to) return 0;
  return to.year * 12 + to.month - (from.year * 12 + from.month) + 1;
}

export function buildPeriodKey(fromMonth: string, toMonth: string): string {
  return `${fromMonth}..${toMonth}`;
}

export function formatContributionPeriodLabel(period: string): string {
  if (period.includes('..')) {
    const [from, to] = period.split('..');
    if (from && to) {
      const count = countInclusiveMonths(from, to);
      return `${formatSingleMonth(from)} – ${formatSingleMonth(to)} (${count} mo)`;
    }
  }

  return formatSingleMonth(period) || period;
}

function formatSingleMonth(yyyyMm: string): string {
  const parsed = parseMonth(yyyyMm);
  if (!parsed) return yyyyMm;
  return new Date(parsed.year, parsed.month - 1, 1).toLocaleDateString('en-IN', {
    month: 'short',
    year: 'numeric',
  });
}

function parseMonth(value: string): { year: number; month: number } | null {
  const match = value.trim().match(MONTH_REGEX);
  if (!match) return null;
  return { year: Number(match[1]), month: Number(match[2]) };
}

export function parseMonthKey(value: string): { year: number; month: number } | null {
  return parseMonth(value);
}

export function formatMonthKey(year: number, month: number): string {
  return `${year}-${String(month).padStart(2, '0')}`;
}

export const MONTH_PICKER_OPTIONS = Array.from({ length: 12 }, (_, index) => {
  const month = index + 1;
  return {
    value: String(month).padStart(2, '0'),
    label: new Date(2000, index, 1).toLocaleDateString('en-IN', { month: 'long' }),
  };
});

export function buildYearOptions(minYear: number, maxYear: number): number[] {
  const years: number[] = [];
  for (let year = minYear; year <= maxYear; year += 1) {
    years.push(year);
  }
  return years;
}

export function getContributionYearRange(date = new Date()): { minYear: number; maxYear: number } {
  const year = date.getFullYear();
  return { minYear: year - 10, maxYear: year + 10 };
}

function toMonthIndex(year: number, month: number): number {
  return year * 12 + month;
}

export function parsePeriodKey(periodKey: string): { fromIndex: number; toIndex: number } | null {
  const trimmed = periodKey.trim();
  let fromMonth: string;
  let toMonth: string;

  if (trimmed.includes('..')) {
    const parts = trimmed.split('..');
    if (parts.length !== 2 || !parts[0] || !parts[1]) {
      return null;
    }
    fromMonth = parts[0];
    toMonth = parts[1];
  } else {
    fromMonth = trimmed;
    toMonth = trimmed;
  }

  const from = parseMonth(fromMonth);
  const to = parseMonth(toMonth);
  if (!from || !to) {
    return null;
  }

  const fromIndex = toMonthIndex(from.year, from.month);
  const toIndex = toMonthIndex(to.year, to.month);
  if (fromIndex > toIndex) {
    return null;
  }

  return { fromIndex, toIndex };
}

export function monthRangesOverlap(
  fromMonth: string,
  toMonth: string,
  existingPeriodKey: string,
): boolean {
  const from = parseMonth(fromMonth);
  const to = parseMonth(toMonth);
  const existing = parsePeriodKey(existingPeriodKey);
  if (!from || !to || !existing) {
    return false;
  }

  const requestFromIndex = toMonthIndex(from.year, from.month);
  const requestToIndex = toMonthIndex(to.year, to.month);
  return requestFromIndex <= existing.toIndex && existing.fromIndex <= requestToIndex;
}

export function findOverlappingPeriod(
  fromMonth: string,
  toMonth: string,
  existingPeriodKeys: string[],
): string | null {
  for (const existing of existingPeriodKeys) {
    if (monthRangesOverlap(fromMonth, toMonth, existing)) {
      return existing;
    }
  }
  return null;
}

export const DURATION_OPTIONS = [1, 2, 3, 4, 6, 9, 12] as const;
