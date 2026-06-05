import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useEffect, useState } from 'react';
import { api, ApiClientError } from '../api/client';
import type { MeetingStatus } from '../api/types';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useAuth, useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { MainStackParamList } from '../navigation/types';
import { todayIsoDate, toApiExpenseDate } from '../utils/expenseDate';

type Props = NativeStackScreenProps<MainStackParamList, 'AddMeeting'>;

export function AddMeetingScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { canManageMeetings } = useAuth();
  const { showSuccess, showError } = useToast();
  const [title, setTitle] = useState('');
  const [meetingDate, setMeetingDate] = useState(todayIsoDate);
  const [location, setLocation] = useState('');
  const [summary, setSummary] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!canManageMeetings) {
      navigation.goBack();
    }
  }, [canManageMeetings, navigation]);

  const onSubmit = async (status: MeetingStatus) => {
    if (!title.trim()) {
      showError('Enter a meeting title');
      return;
    }
    setLoading(true);
    try {
      const meeting = await api.createMeeting(
        groupId,
        {
          title: title.trim(),
          meetingDate: toApiExpenseDate(meetingDate),
          location: location.trim() || null,
          summary: summary.trim() || null,
          status,
        },
        token,
        memberId,
      );
      showSuccess(status === 'Draft' ? 'Meeting saved as draft' : 'Meeting published');
      navigation.replace('MeetingDetail', { meetingId: meeting.id });
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to create meeting');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen title="Record meeting" subtitle="Save as draft, then tap backlog topics to record discussion">
      <Input label="Title" value={title} onChangeText={setTitle} placeholder="Monthly committee meeting" />
      <Input label="Meeting date" value={meetingDate} onChangeText={setMeetingDate} placeholder="YYYY-MM-DD" />
      <Input label="Location" value={location} onChangeText={setLocation} placeholder="Community hall" />
      <Input label="Summary" value={summary} onChangeText={setSummary} placeholder="Optional overview" multiline />
      <Button
        label={loading ? 'Saving…' : 'Save as draft'}
        onPress={() => void onSubmit('Draft')}
        disabled={loading}
      />
      <Button
        label="Save & publish"
        variant="secondary"
        onPress={() => void onSubmit('Published')}
        disabled={loading}
      />
    </Screen>
  );
}
