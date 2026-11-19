import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../lib/api';

export interface ApprovalRecord {
  id: string;
  decisionId: string;
  status: string;
  assignedToUserId: string | null;
  expiresAt: string | null;
  createdAt: string;
}

export const useApprovals = (accountId?: string) =>
  useQuery({
    queryKey: ['approvals', accountId ?? 'all'],
    queryFn: async () => {
      const { data } = await apiClient.get<ApprovalRecord[]>('/approvals', {
        params: accountId ? { accountId } : undefined,
      });
      return data;
    },
    refetchInterval: 30_000,
  });

export const useResolveApproval = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      action,
      note,
      reason,
    }: {
      id: string;
      action: 'approve' | 'reject';
      note: string;
      reason?: string;
    }) => {
      const { data } = await apiClient.post<ApprovalRecord>(
        `/approvals/${id}/${action}`,
        action === 'approve' ? { note } : { note, reason }
      );
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['approvals'] }),
  });
};
