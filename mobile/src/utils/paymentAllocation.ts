import type { PendingContributionItem } from '../api/types';

export type PaymentAllocationPreview = {
  period: string;
  amountApplied: number;
  remainingAfter: number;
  isAdvance?: boolean;
};

export const ADVANCE_CREDIT_PERIOD = 'Advance credit';

export function computePaymentAllocation(
  items: PendingContributionItem[],
  amount: number,
): PaymentAllocationPreview[] {
  if (amount <= 0) {
    return [];
  }

  const ordered = [...items].sort((a, b) => a.period.localeCompare(b.period));
  let amountLeft = amount;
  const allocations: PaymentAllocationPreview[] = [];

  for (const item of ordered) {
    if (amountLeft <= 0) {
      break;
    }

    const apply = Math.min(amountLeft, item.remainingAmount);
    if (apply <= 0) {
      continue;
    }

    allocations.push({
      period: item.period,
      amountApplied: apply,
      remainingAfter: item.remainingAmount - apply,
    });
    amountLeft -= apply;
  }

  if (amountLeft > 0) {
    allocations.push({
      period: ADVANCE_CREDIT_PERIOD,
      amountApplied: amountLeft,
      remainingAfter: 0,
      isAdvance: true,
    });
  }

  return allocations;
}
