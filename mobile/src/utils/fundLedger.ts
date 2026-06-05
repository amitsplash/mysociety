import type { GroupFundType, FundLedgerLine } from '../api/types';

/** Oldest first; running balance is per fund (from server). */
export function prepareFundLedgerLines(lines: FundLedgerLine[]): FundLedgerLine[] {
  return [...lines].sort(
    (a, b) => new Date(a.transactionDate).getTime() - new Date(b.transactionDate).getTime(),
  );
}

export function filterFundLedgerByFund(
  lines: FundLedgerLine[],
  fundType: GroupFundType,
): FundLedgerLine[] {
  return lines.filter((line) => line.fundType === fundType);
}

export function isCorpusPending(member: {
  corpusAmount: number;
  corpusPaidAt?: string | null;
}): boolean {
  return member.corpusAmount > 0 && !member.corpusPaidAt;
}
