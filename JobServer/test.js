const http = require("http");
const fs = require("fs");
const path = require("path");
const { server, DATA } = require("./server");

function request(method, url, body) {
  return new Promise((resolve, reject) => {
    const req = http.request(url, {
      method,
      headers: body ? { "Content-Type": "application/json" } : {}
    }, res => {
      let s = "";
      res.on("data", c => s += c);
      res.on("end", () => resolve({ status: res.statusCode, body: s ? JSON.parse(s) : null }));
    });
    req.on("error", reject);
    if (body) req.end(JSON.stringify(body)); else req.end();
  });
}

(async () => {
  fs.writeFileSync(DATA, JSON.stringify({ jobs: [] }));
  await new Promise(r => server.listen(0, r));
  const port = server.address().port;
  const base = `http://127.0.0.1:${port}`;

  const jobs = [
    {id:"late",client:"Late",year:2025,dueDate:"2026-12-01T00:00:00Z",fields:{Box2:"2"}},
    {id:"soon",client:"Soon",year:2025,dueDate:"2026-09-02T00:00:00Z",fields:{Box2:"1"}},
    {id:"mid",client:"Mid",year:2025,dueDate:"2026-10-01T00:00:00Z",fields:{Box2:"3"}}
  ];
  for (const j of jobs) await request("POST", base + "/jobs", j);

  const [a,b] = await Promise.all([request("GET", base+"/claim"), request("GET", base+"/claim")]);
  const claimed = [a,b].filter(x => x.status === 200).map(x => x.body.id);
  if (claimed.length !== 2 || new Set(claimed).size !== 2 || !claimed.includes("soon"))
    throw new Error("urgency/concurrency test failed: " + JSON.stringify(claimed));

  console.log("PASS: urgency and concurrent claims are unique");
  server.close();
})().catch(e => { console.error(e); process.exit(1); });
