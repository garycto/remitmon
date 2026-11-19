import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../lib/api';

export interface ExpectedPayment {
  id: string;
  source: string;
  sourceRef: string;
  counterpartyName: string;
  amount: number;
  dueDate: string;
  status: string;
  matchedPaymentEventId: string | null;
}

export interface PaymentActivity {
  id: string;
  accountId: string;
  accountName: string;
  rail: string;
  direction: string;
  initiation: string;
  amount: number;
  currency: string;
  counterpartyRef: string | null;
  memo: string | null;
  reference: string | null;
  invoiceRef: string | null;
  eventTime: string;
  settlementDate: string | null;
  matchStatus: string;
  matchedObligationId: string | null;
  matchedObligationRef: string | null;
  outcome: string;
  policyId: string | null;
  policyName: string;
  ruleHits: string[];
}

interface DashboardActivity {
  expectedPayments: ExpectedPayment[];
  payments: PaymentActivity[];
}

interface ModernTreasuryPayment {
  id: string;
  externalEventId: string | null;
  accountId: string;
  rail: string;
  direction: string;
  initiation: string;
  amount: number;
  currency: string;
  counterpartyRef: string | null;
  reference: string | null;
  memo: string | null;
  invoiceRef: string | null;
  eventTime: string;
  settlementDate: string | null;
  accountName?: string;
}

const mapModernTreasuryPayment = (payment: ModernTreasuryPayment): PaymentActivity => ({
  id: payment.externalEventId ?? payment.id,
  accountId: payment.accountId,
  accountName: 'Modern Treasury Sandbox',
  rail: payment.rail,
  direction: payment.direction,
  initiation: payment.initiation,
  amount: payment.amount,
  currency: payment.currency,
  counterpartyRef: payment.counterpartyRef,
  memo: payment.memo,
  reference: payment.reference,
  invoiceRef: payment.invoiceRef,
  eventTime: payment.eventTime,
  settlementDate: payment.settlementDate,
  matchStatus: 'Unmatched',
  matchedObligationId: null,
  matchedObligationRef: null,
  outcome: 'Review',
  policyId: null,
  policyName: 'External activity',
  ruleHits: ['Awaiting RemitMon policy evaluation'],
});

export const useDashboardActivity = (accountId?: string) => {
  return useQuery({
    queryKey: ['dashboard', 'activity', accountId ?? 'all'],
    queryFn: async () => {
      const accountParams = accountId ? { accountId } : undefined;
      const mtParams = accountId ? { accountId } : undefined;

      const [dashboardResult, mtOrdersResult, mtIncomingResult] = await Promise.allSettled([
        apiClient.get<DashboardActivity>('/dashboard/activity', { params: accountParams }),
        apiClient.get<ModernTreasuryPayment[]>('/modern-treasury/payment-orders', { params: mtParams }),
        apiClient.get<ModernTreasuryPayment[]>('/modern-treasury/incoming-payment-details', { params: mtParams }),
      ]);

      if (dashboardResult.status === 'rejected'
          && mtOrdersResult.status === 'rejected'
          && mtIncomingResult.status === 'rejected') {
        throw new Error('Unable to load Supabase demo activity or Modern Treasury sandbox activity.');
      }

      const dashboard = dashboardResult.status === 'fulfilled' ? dashboardResult.value.data : {
        expectedPayments: [],
        payments: [],
      };
      const mtPayments = [
        ...(mtOrdersResult.status === 'fulfilled' ? mtOrdersResult.value.data : []),
        ...(mtIncomingResult.status === 'fulfilled' ? mtIncomingResult.value.data : []),
      ].map(mapModernTreasuryPayment);

      const seen = new Set(dashboard.payments.map((payment) =>
        `${payment.reference ?? ''}:${payment.eventTime}:${payment.amount}`));
      const uniqueMtPayments = mtPayments.filter((payment) => {
        const key = `${payment.reference ?? ''}:${payment.eventTime}:${payment.amount}`;
        if (seen.has(key)) return false;
        seen.add(key);
        return true;
      });

      return {
        expectedPayments: dashboard.expectedPayments,
        payments: [...dashboard.payments, ...uniqueMtPayments]
          .sort((left, right) => new Date(right.eventTime).getTime() - new Date(left.eventTime).getTime()),
      } satisfies DashboardActivity;
    },
  });
};
