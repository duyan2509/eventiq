import React, { useState, useEffect } from 'react';
import { Typography, Tabs } from 'antd';
import { DollarOutlined } from '@ant-design/icons';
import { revenueAPI } from '../services/api';
import dayjs from 'dayjs';
import RevenueTab from '../components/Admin/RevenueTab';
import PayoutTab from '../components/Admin/PayoutTab';

const { Title } = Typography;

const AdminRevenueManagement = () => {
    const [loading, setLoading] = useState(true);
    const [reportData, setReportData] = useState(null);
    const [selectedMonth, setSelectedMonth] = useState(dayjs().month() + 1);
    const [selectedYear, setSelectedYear] = useState(dayjs().year());

    useEffect(() => {
        fetchRevenueReport();
    }, [selectedMonth, selectedYear]);

    const fetchRevenueReport = async () => {
        try {
            setLoading(true);
            const data = await revenueAPI.getAdminRevenueReport(selectedMonth, selectedYear);
            setReportData(data);
        } catch (error) {
            console.error('Error fetching revenue report:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleMonthChange = (month) => {
        setSelectedMonth(month);
    };

    const handleYearChange = (year) => {
        setSelectedYear(year);
    };

    const tabItems = [
        {
            key: 'revenue',
            label: 'Revenue',
            children: (
                <RevenueTab
                    reportData={reportData}
                    loading={loading}
                    selectedMonth={selectedMonth}
                    selectedYear={selectedYear}
                    onMonthChange={handleMonthChange}
                    onYearChange={handleYearChange}
                />
            ),
        },
        {
            key: 'payout',
            label: 'Payout',
            children: <PayoutTab />,
        },
    ];

    return (
        <div style={{ padding: '24px' }}>
            <Title level={2}>
                Revenue Management
            </Title>
            <Tabs defaultActiveKey="revenue" items={tabItems} />
        </div>
    );
};

export default AdminRevenueManagement;
