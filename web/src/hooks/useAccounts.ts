import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../lib/api';

export interface Account {
  id: string;
  businessCustomerId: string;
  bankId: string;
  displayName: string;
  maskedAccountNumber: string;
  status: string;
  mode: string;
  createdAt: string;
  updatedAt: string;
}

export const useAccounts = () => {
  return useQuery({
    queryKey: ['accounts'],
    queryFn: async () => {
      const { data } = await apiClient.get<Account[]>('/accounts');
      return data;
    },
  });
};

export const useAccount = (id: string) => {
  return useQuery({
    queryKey: ['accounts', id],
    queryFn: async () => {
      const { data } = await apiClient.get<Account>(`/accounts/${id}`);
      return data;
    },
    enabled: !!id,
  });
};

export const useCreateAccount = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (account: Partial<Account>) => {
      const { data } = await apiClient.post<Account>('/accounts', account);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
    },
  });
};

export const useUpdateAccountMode = (id: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (mode: string) => {
      const { data } = await apiClient.patch<Account>(`/accounts/${id}/mode`, { mode });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts', id] });
    },
  });
};
