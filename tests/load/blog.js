import http from 'k6/http';
import { check } from 'k6';

const baseUrl = (__ENV.BLOG_API_URL || 'http://127.0.0.1:5000').replace(/\/$/, '');

export const options = {
  summaryTrendStats: ['avg', 'med', 'min', 'max', 'p(90)', 'p(95)', 'p(99)'],
  scenarios: {
    blog_read: {
      executor: 'ramping-arrival-rate',
      startRate: 2,
      timeUnit: '1s',
      preAllocatedVUs: 10,
      maxVUs: 100,
      stages: [
        { target: 10, duration: '30s' },
        { target: 10, duration: '1m' },
        { target: 0, duration: '10s' },
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],
  },
};

export default function () {
  const response = http.get(`${baseUrl}/api/posts`);
  check(response, { 'blog posts returns 200': (r) => r.status === 200 });
}

export function handleSummary(data) {
  return { 'artifacts/load-blog.json': JSON.stringify(data, null, 2) };
}
