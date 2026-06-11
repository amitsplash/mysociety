import AsyncStorage from '@react-native-async-storage/async-storage';
import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api } from '../api/client';
import type { LoginResponse, MembershipSummary } from '../api/types';

const STORAGE_KEYS = {
  token: '@mysociety/token',
  user: '@mysociety/user',
  memberships: '@mysociety/memberships',
  activeMemberId: '@mysociety/activeMemberId',
  activeGroupId: '@mysociety/activeGroupId',
};

interface AuthState {
  token: string | null;
  user: LoginResponse['user'] | null;
  memberships: MembershipSummary[];
  activeMemberId: string | null;
  activeGroupId: string | null;
  isLoading: boolean;
}

interface AuthContextValue extends AuthState {
  login: (phone: string, password: string) => Promise<void>;
  register: (phone: string, email: string, name: string, password: string) => Promise<void>;
  completeLogin: (response: LoginResponse) => Promise<void>;
  logout: () => Promise<void>;
  setActiveMembership: (memberId: string, groupId: string) => Promise<void>;
  activeMembership: MembershipSummary | null;
  isAdmin: boolean;
  isCommitteeMember: boolean;
  canManageMeetings: boolean;
  refreshCommitteeStatus: () => Promise<void>;
  refreshSession: () => Promise<void>;
  addMembership: (membership: MembershipSummary) => Promise<void>;
  removeGroupFromSession: (groupId: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>({
    token: null,
    user: null,
    memberships: [],
    activeMemberId: null,
    activeGroupId: null,
    isLoading: true,
  });
  const [isCommitteeMember, setIsCommitteeMember] = useState(false);

  const clearStoredSession = async () => {
    await Promise.all(Object.values(STORAGE_KEYS).map((key) => AsyncStorage.removeItem(key)));
  };

  const persist = async (payload: Partial<AuthState>) => {
    if (payload.token !== undefined) {
      if (payload.token) await AsyncStorage.setItem(STORAGE_KEYS.token, payload.token);
      else await AsyncStorage.removeItem(STORAGE_KEYS.token);
    }
    if (payload.user !== undefined) {
      if (payload.user) await AsyncStorage.setItem(STORAGE_KEYS.user, JSON.stringify(payload.user));
      else await AsyncStorage.removeItem(STORAGE_KEYS.user);
    }
    if (payload.memberships !== undefined) {
      const memberships = Array.isArray(payload.memberships) ? payload.memberships : [];
      if (memberships.length > 0) {
        await AsyncStorage.setItem(STORAGE_KEYS.memberships, JSON.stringify(memberships));
      } else {
        await AsyncStorage.removeItem(STORAGE_KEYS.memberships);
      }
    }
    if (payload.activeMemberId !== undefined) {
      if (payload.activeMemberId)
        await AsyncStorage.setItem(STORAGE_KEYS.activeMemberId, payload.activeMemberId);
      else await AsyncStorage.removeItem(STORAGE_KEYS.activeMemberId);
    }
    if (payload.activeGroupId !== undefined) {
      if (payload.activeGroupId)
        await AsyncStorage.setItem(STORAGE_KEYS.activeGroupId, payload.activeGroupId);
      else await AsyncStorage.removeItem(STORAGE_KEYS.activeGroupId);
    }
  };

  const refreshSession = useCallback(async () => {
    const [token, userJson, membershipsJson, activeMemberId, activeGroupId] = await Promise.all([
      AsyncStorage.getItem(STORAGE_KEYS.token),
      AsyncStorage.getItem(STORAGE_KEYS.user),
      AsyncStorage.getItem(STORAGE_KEYS.memberships),
      AsyncStorage.getItem(STORAGE_KEYS.activeMemberId),
      AsyncStorage.getItem(STORAGE_KEYS.activeGroupId),
    ]);

    const parsedUser = userJson ? JSON.parse(userJson) : null;
    let memberships: MembershipSummary[] = [];
    if (membershipsJson) {
      try {
        const parsed = JSON.parse(membershipsJson);
        memberships = Array.isArray(parsed) ? parsed : [];
      } catch {
        memberships = [];
      }
    }
    setState({
      token,
      user: parsedUser,
      memberships,
      activeMemberId,
      activeGroupId,
      isLoading: false,
    });
  }, []);

  useEffect(() => {
    refreshSession();
  }, [refreshSession]);

  const completeLogin = useCallback(async (response: LoginResponse) => {
    const memberships = Array.isArray(response.memberships) ? response.memberships : [];
    const first = memberships[0];
    const next: Partial<AuthState> = {
      token: response.token,
      user: response.user,
      memberships,
      activeMemberId: first?.memberId ?? null,
      activeGroupId: first?.groupId ?? null,
    };
    await persist(next);
    setState((s) => ({ ...s, ...next, isLoading: false }));
  }, []);

  const login = useCallback(
    async (phone: string, password: string) => {
      const response = await api.login({ phone, password });
      await completeLogin(response);
    },
    [completeLogin],
  );

  const register = useCallback(
    async (phone: string, email: string, name: string, password: string) => {
      const response = await api.register({ phone, email, name, password });
      await completeLogin(response);
    },
    [completeLogin],
  );

  const logout = useCallback(async () => {
    const clearedState: AuthState = {
      token: null,
      user: null,
      memberships: [],
      activeMemberId: null,
      activeGroupId: null,
      isLoading: false,
    };
    setIsCommitteeMember(false);
    setState(clearedState);
    try {
      await clearStoredSession();
    } catch {
      // In-memory state already cleared; navigation should still switch to login.
    }
  }, []);

  const setActiveMembership = useCallback(async (memberId: string, groupId: string) => {
    await persist({ activeMemberId: memberId, activeGroupId: groupId });
    setState((s) => ({ ...s, activeMemberId: memberId, activeGroupId: groupId }));
  }, []);

  const activeMembership = useMemo(() => {
    const memberships = Array.isArray(state.memberships) ? state.memberships : [];
    return memberships.find((m) => m.memberId === state.activeMemberId) ?? null;
  }, [state.memberships, state.activeMemberId]);

  const isAdmin = activeMembership?.role === 'Admin';
  const canManageMeetings = isAdmin || isCommitteeMember;

  const refreshCommitteeStatus = useCallback(async () => {
    const { token, activeMemberId, activeGroupId } = state;
    if (!token || !activeMemberId || !activeGroupId) {
      setIsCommitteeMember(false);
      return;
    }

    try {
      const roster = await api.getCommitteeMembers(activeGroupId, token, activeMemberId);
      setIsCommitteeMember(roster.some((entry) => entry.memberId === activeMemberId));
    } catch {
      setIsCommitteeMember(false);
    }
  }, [state.token, state.activeMemberId, state.activeGroupId]);

  useEffect(() => {
    void refreshCommitteeStatus();
  }, [refreshCommitteeStatus]);

  const addMembership = useCallback(async (membership: MembershipSummary) => {
    let memberships: MembershipSummary[] = [];
    let activeMemberId: string | null = null;
    let activeGroupId: string | null = null;

    setState((s) => {
      memberships = [...s.memberships, membership];
      activeMemberId = membership.memberId;
      activeGroupId = membership.groupId;
      return {
        ...s,
        memberships,
        activeMemberId,
        activeGroupId,
      };
    });

    await persist({ memberships, activeMemberId, activeGroupId });
  }, []);

  const removeGroupFromSession = useCallback(async (groupId: string) => {
    let memberships: MembershipSummary[] = [];
    let activeMemberId: string | null = null;
    let activeGroupId: string | null = null;

    setState((s) => {
      const current = Array.isArray(s.memberships) ? s.memberships : [];
      memberships = current.filter((m) => m.groupId !== groupId);
      const first = memberships[0];
      activeMemberId = first?.memberId ?? null;
      activeGroupId = first?.groupId ?? null;
      return {
        ...s,
        memberships,
        activeMemberId,
        activeGroupId,
      };
    });

    await persist({
      memberships,
      activeMemberId,
      activeGroupId,
    });
  }, []);

  const value = useMemo(
    () => ({
      ...state,
      login,
      register,
      completeLogin,
      logout,
      setActiveMembership,
      activeMembership,
      isAdmin,
      isCommitteeMember,
      canManageMeetings,
      refreshCommitteeStatus,
      refreshSession,
      addMembership,
      removeGroupFromSession,
    }),
    [
      state,
      login,
      register,
      completeLogin,
      logout,
      setActiveMembership,
      activeMembership,
      isAdmin,
      isCommitteeMember,
      canManageMeetings,
      refreshCommitteeStatus,
      refreshSession,
      addMembership,
      removeGroupFromSession,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

export function useSession() {
  const { token, activeMemberId, activeGroupId } = useAuth();
  if (!token || !activeMemberId || !activeGroupId) {
    throw new Error('Session not ready');
  }
  return { token, memberId: activeMemberId, groupId: activeGroupId };
}
