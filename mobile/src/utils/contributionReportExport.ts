import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { Platform } from 'react-native';
import { formatContributionPeriodLabel } from './contributionMonthRange';

export type ContributionReportRow = {
  memberName: string;
  generated: number;
  paid: number;
  pending: number;
  statusLabel: string;
};

export type ContributionPeriodExport = {
  period: string;
  rows: ContributionReportRow[];
  totalGenerated: number;
  totalPaid: number;
  totalPending: number;
};

function csvCell(value: string | number): string {
  const text = String(value);
  if (/[",\n\r]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }
  return text;
}

function sanitizeFilenamePart(value: string): string {
  return value.replace(/[^a-zA-Z0-9._-]+/g, '_').replace(/_+/g, '_');
}

export function buildContributionPeriodCsv(groupName: string, block: ContributionPeriodExport): string {
  const periodLabel = formatContributionPeriodLabel(block.period);
  const lines = [
    `Group,${csvCell(groupName)}`,
    `Period,${csvCell(periodLabel)}`,
    `Period key,${csvCell(block.period)}`,
    '',
    'Member,Generated (INR),Paid (INR),Pending (INR),Status',
    ...block.rows.map((row) =>
      [
        csvCell(row.memberName),
        csvCell(row.generated),
        csvCell(row.paid),
        csvCell(row.pending),
        csvCell(row.statusLabel),
      ].join(','),
    ),
    [
      csvCell('Total'),
      csvCell(block.totalGenerated),
      csvCell(block.totalPaid),
      csvCell(block.totalPending),
      csvCell(''),
    ].join(','),
  ];

  return `\uFEFF${lines.join('\n')}`;
}

export async function downloadContributionPeriodCsv(
  groupName: string,
  block: ContributionPeriodExport,
): Promise<void> {
  const csv = buildContributionPeriodCsv(groupName, block);
  const filename = `contributions_${sanitizeFilenamePart(block.period)}.csv`;

  if (Platform.OS === 'web') {
    if (typeof document === 'undefined') {
      throw new Error('Download is not supported in this environment.');
    }
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
    return;
  }

  const directory = FileSystem.cacheDirectory;
  if (!directory) {
    throw new Error('File storage is not available on this device.');
  }

  const fileUri = `${directory}${filename}`;
  await FileSystem.writeAsStringAsync(fileUri, csv, {
    encoding: FileSystem.EncodingType.UTF8,
  });

  const canShare = await Sharing.isAvailableAsync();
  if (!canShare) {
    throw new Error('Sharing is not available on this device.');
  }

  await Sharing.shareAsync(fileUri, {
    mimeType: 'text/csv',
    dialogTitle: `Download ${formatContributionPeriodLabel(block.period)}`,
    UTI: 'public.comma-separated-values-text',
  });
}
