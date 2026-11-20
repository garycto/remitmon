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
  achInstantEligible: boolean;
  convertedToInstant: boolean;
}

interface DashboardActivity {
  expectedPayments: ExpectedPayment[];
  payments: PaymentActivity[];
  achInstantOpportunity: {
    eligible: number;
    converted: number;
    remaining: number;
  };
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
  achInstantEligible: payment.rail.toLowerCase() === 'ach',
  convertedToInstant: false,
});

export const useDashboardActivity = (accountId?: string, accountIds: string[] = []) => {
  return useQuery({
    queryKey: ['dashboard', 'activity', accountId ?? 'all', accountIds],
    queryFn: async () => {
      const ids = accountId ? [accountId] : accountIds;
      const requests = ids.length ? ids : [undefined];
      const results = await Promise.allSettled(requests.flatMap((id) => {
        const params = id ? { accountId: id } : undefined;
        return [
          apiClient.get<DashboardActivity>('/dashboard/activity', { params }),
          apiClient.get<ModernTreasuryPayment[]>('/modern-treasury/payment-orders', { params }),
          apiClient.get<ModernTreasuryPayment[]>('/modern-treasury/incoming-payment-details', { params }),
        ];
      }));

      if (results.every((result) => result.status === 'rejected')) {
        throw new Error('Unable to load Supabase demo activity or Modern Treasury sandbox activity.');
      }

      const dashboards = results.filter((result, index) =>
        result.status === 'fulfilled' && index % 3 === 0
      ) as PromiseFulfilledResult<{ data: DashboardActivity }>[];
      const mtPayments = results
        .filter((result, index) => result.status === 'fulfilled' && index % 3 !== 0)
        .flatMap((result) => (result as PromiseFulfilledResult<{ data: ModernTreasuryPayment[] }>).value.data)
        .map(mapModernTreasuryPayment);

      const dashboardPayments = dashboards.flatMap((result) => result.value.data.payments);
      const expectedPayments = dashboards.flatMap((result) => result.value.data.expectedPayments);
      const seen = new Set(dashboardPayments.map((payment) =>
        `${payment.reference ?? ''}:${payment.eventTime}:${payment.amount}`));
      const uniqueMtPayments = mtPayments.filter((payment) => {
        const key = `${payment.reference ?? ''}:${payment.eventTime}:${payment.amount}`;
        if (seen.has(key)) return false;
        seen.add(key);
        return true;
      });

      return {
        expectedPayments,
        payments: [...dashboardPayments, ...uniqueMtPayments]
          .sort((left, right) => new Date(right.eventTime).getTime() - new Date(left.eventTime).getTime()),
        achInstantOpportunity: dashboards.reduce(
          (total, result) => {
            const opportunity = result.value.data.achInstantOpportunity;
            return opportunity
              ? {
                  eligible: total.eligible + opportunity.eligible,
                  converted: total.converted + opportunity.converted,
                  remaining: total.remaining + opportunity.remaining,
                }
              : total;
          },
          { eligible: 0, converted: 0, remaining: 0 },
        ),
      } satisfies DashboardActivity;
    },
  });
};
