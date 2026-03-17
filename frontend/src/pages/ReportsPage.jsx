import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import Sidebar from '../components/Sidebar';
import { 
  Activity, 
  TrendingUp, 
  BarChart3, 
  PieChart, 
  Zap, 
  Target, 
  Users, 
  Clock,
  ArrowUpRight,
  ArrowDownRight
} from 'lucide-react';
import { dashboardService } from '../services/api';
import { cn } from '../lib/utils';

const ReportsPage = ({ user, onLogout }) => {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchStats();
  }, []);

  const fetchStats = async () => {
    try {
      const res = await dashboardService.getGlobalStats();
      setStats(res.data);
    } catch (err) {
      console.error("Failed to fetch analytics", err);
    } finally {
      setLoading(false);
    }
  };

  const completionRate = stats?.totalTasks > 0 
    ? Math.round((stats.completedTasks / stats.totalTasks) * 100) 
    : 0;

  return (
    <div className="min-h-screen bg-transparent text-text flex relative">
      <Sidebar user={user} onLogout={onLogout} />
      
      <main className="flex-grow ml-72 p-12 relative z-10 w-full overflow-y-auto h-screen">
        <motion.header 
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-10"
        >
          <div className="flex items-center gap-4 mb-2">
            <div className="w-12 h-12 rounded-2xl bg-primary/10 flex items-center justify-center text-primary shadow-sm border border-primary/20">
              <Activity size={24} />
            </div>
            <h1 className="text-4xl font-heading font-extrabold bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Intelligence & Analytics</h1>
          </div>
          <p className="text-text-muted font-medium ml-1">Advanced insights into project velocity, team output, and resource efficiency.</p>
        </motion.header>

        {loading ? (
          <div className="h-[60vh] flex flex-col items-center justify-center gap-4 text-text-muted">
             <div className="relative">
                <Activity className="animate-spin text-primary" size={48} />
                <div className="absolute inset-0 blur-xl bg-primary/20 animate-pulse"></div>
             </div>
             <p className="font-bold animate-pulse text-lg">Crunching data points...</p>
          </div>
        ) : (
          <div className="space-y-8">
            {/* Quick Metrics */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
               <ReportStatCard 
                 title="Success Rate" 
                 value={`${completionRate}%`} 
                 icon={<Target className="text-green-500" />} 
                 trend="+4.2%" 
                 description="Tasks completed vs total"
               />
               <ReportStatCard 
                 title="Avg Velocity" 
                 value="8.4" 
                 icon={<Zap className="text-orange-500" />} 
                 trend="+1.5" 
                 description="Tasks per agent/day"
               />
               <ReportStatCard 
                 title="Throughput" 
                 value={stats?.completedTasks || 0} 
                 icon={<BarChart3 className="text-primary" />} 
                 trend="+12" 
                 description="Completed this week"
               />
               <ReportStatCard 
                 title="Lead Time" 
                 value="2.3d" 
                 icon={<Clock className="text-secondary" />} 
                 trend="-0.5" 
                 description="Time to completion"
                 inverse
               />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              {/* Velocity Chart Placeholder - High Polish CSS */}
              <motion.div 
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ delay: 0.2 }}
                className="lg:col-span-2 glass-card p-8 border-primary/10"
              >
                <div className="flex items-center justify-between mb-8">
                   <div>
                      <h3 className="text-xl font-bold mb-1">Project Velocity</h3>
                      <p className="text-xs text-text-muted font-medium">Weekly task completion trend</p>
                   </div>
                   <div className="flex gap-2">
                       <span className="glass-tab px-3 py-1 text-[10px] font-bold uppercase tracking-widest border border-primary/10">7 Days</span>
                       <span className="glass-tab-active px-3 py-1 text-[10px] font-bold uppercase tracking-widest border border-primary/20">30 Days</span>
                   </div>
                </div>
                
                <div className="h-64 flex items-end justify-between gap-4 px-4">
                   {[40, 65, 45, 80, 55, 90, 70].map((h, i) => (
                      <div key={i} className="flex-grow flex flex-col items-center group relative">
                         <motion.div 
                           initial={{ height: 0 }}
                           animate={{ height: `${h}%` }}
                           transition={{ delay: 0.3 + (i * 0.1), duration: 1 }}
                           className="w-full max-w-[40px] bg-gradient-to-t from-primary/20 via-primary/60 to-primary rounded-t-xl group-hover:scale-110 transition-transform cursor-pointer relative"
                         >
                            <div className="absolute -top-10 left-1/2 -translate-x-1/2 bg-text text-white text-[10px] font-bold px-2 py-1 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">
                               {h} tasks
                            </div>
                         </motion.div>
                         <span className="text-[10px] font-bold text-text-muted mt-4">Day {i+1}</span>
                      </div>
                   ))}
                </div>
              </motion.div>

              {/* Status Distribution */}
              <motion.div 
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.4 }}
                className="glass-card p-8 border-primary/10"
              >
                <h3 className="text-xl font-bold mb-1">Task Breakdown</h3>
                <p className="text-xs text-text-muted font-medium mb-8">Current repository status</p>
                
                <div className="space-y-6">
                   <MiniProgress label="Completed" value={stats?.completedTasks || 0} total={stats?.totalTasks || 1} color="bg-green-500" />
                   <MiniProgress label="In Progress" value={stats?.inProgressTasks || 0} total={stats?.totalTasks || 1} color="bg-primary" />
                   <MiniProgress label="Pending" value={stats?.totalTasks - (stats?.completedTasks + stats?.inProgressTasks) || 0} total={stats?.totalTasks || 1} color="bg-orange-400" />
                   <MiniProgress label="Blocked/Overdue" value={stats?.overdueTasks || 0} total={stats?.totalTasks || 1} color="bg-red-500" />
                </div>

                <div className="mt-10 p-6 rounded-2xl bg-gradient-to-br from-primary/5 to-secondary/5 border border-primary/10">
                   <div className="flex items-center gap-3 mb-3">
                      <div className="w-8 h-8 rounded-lg bg-primary/10 flex items-center justify-center text-primary">
                         <TrendingUp size={16} />
                      </div>
                      <span className="text-sm font-bold">Optimization Tip</span>
                   </div>
                   <p className="text-xs text-text-muted font-medium leading-relaxed">
                      AI agents suggest reassigning <span className="text-primary font-bold">3 tasks</span> from overloaded members to improve lead time.
                   </p>
                </div>
              </motion.div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
};

const ReportStatCard = ({ title, value, icon, trend, description, inverse = false }) => (
  <motion.div 
    whileHover={{ y: -5, scale: 1.02 }}
    className="glass-card p-6 border-primary/10 hover:shadow-xl hover:shadow-primary/5 transition-all"
  >
     <div className="flex items-center justify-between mb-4">
        <div className="w-10 h-10 rounded-xl bg-white/60 border border-primary/10 flex items-center justify-center shadow-sm">
           {icon}
        </div>
        <div className={cn(
          "flex items-center gap-1 text-[10px] font-black px-2 py-1 rounded-full border shadow-sm",
          inverse 
            ? trend.startsWith('-') ? "bg-green-50 text-green-600 border-green-100" : "bg-red-50 text-red-600 border-red-100"
            : trend.startsWith('+') ? "bg-green-50 text-green-600 border-green-100" : "bg-red-50 text-red-600 border-red-100"
        )}>
           {trend.startsWith('+') ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}
           {trend}
        </div>
     </div>
     <p className="text-[10px] font-bold text-text-muted uppercase tracking-widest mb-1">{title}</p>
     <h4 className="text-2xl font-heading font-extrabold text-text mb-2 tracking-tight">{value}</h4>
     <p className="text-[10px] font-medium text-text-muted italic">{description}</p>
  </motion.div>
);

const MiniProgress = ({ label, value, total, color }) => {
  const percentage = Math.round((value / total) * 100) || 0;
  return (
    <div className="space-y-2">
       <div className="flex justify-between text-[10px] font-black uppercase tracking-wider">
          <span className="text-text-muted">{label}</span>
          <span className="text-text">{value} ({percentage}%)</span>
       </div>
       <div className="h-2 bg-white/40 border border-primary/5 rounded-full overflow-hidden">
          <motion.div 
            initial={{ width: 0 }}
            animate={{ width: `${percentage}%` }}
            transition={{ duration: 1 }}
            className={cn("h-full rounded-full transition-all", color)}
          />
       </div>
    </div>
  );
};

export default ReportsPage;
